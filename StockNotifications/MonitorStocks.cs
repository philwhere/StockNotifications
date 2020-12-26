using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockNotifications.Clients.Interfaces;
using StockNotifications.Extensions;
using StockNotifications.Models.ExternalApis.RapidApiYahooFinance;
using StockNotifications.Models.TableStorageEntities;

namespace StockNotifications
{
    public class MonitorStocks
    {
        private readonly AppSettings _appSettings;
        private readonly IYahooFinanceClient _yahooFinanceClient;
        private readonly ISlackClient _slackClient;

        private readonly CloudTable _historyTable;
        private readonly CloudTable _monitoredStocksTable;
        private static string NzTodayDateString => DateTime.Now.ToTableDateFormat();

        public MonitorStocks(IOptions<AppSettings> appSettings, IYahooFinanceClient yahooFinanceClient, ISlackClient slackClient)
        {
            _appSettings = appSettings.Value;
            _yahooFinanceClient = yahooFinanceClient;
            _slackClient = slackClient;

            var tableClient = CreateTableClient();
            _historyTable = tableClient.GetTableReference("NotificationHistory");
            _monitoredStocksTable = tableClient.GetTableReference("StocksToMonitor");
        }

        [FunctionName("MonitorStocks")]
        public async Task Run([TimerTrigger("%MonitorStocksSchedule%")] TimerInfo timer, ILogger log)
        {
            var stocksToMonitor = GetStocksToMonitor();
            var regionalStockGroups = GroupStocksByRegion(stocksToMonitor);
            var allNotificationsToday = GetAllNotificationsToday();
            foreach (var regionalStockGroup in regionalStockGroups)
            {
                var stockQuotes = await GetStockQuotes(regionalStockGroup);
                var pairedStockDetails = JoinStockDetails(regionalStockGroup, stockQuotes, allNotificationsToday);

                foreach (var (monitoredStock, quote, notificationHistory) in pairedStockDetails)
                {
                    var currentPrice = quote.RegularMarketPrice;
                    if (CurrentPriceUnderThreshold(notificationHistory, currentPrice, monitoredStock))
                        await TriggerAlert(monitoredStock, currentPrice, quote.LongName);
                }
            }
        }


        private IReadOnlyCollection<NotificationHistory> GetAllNotificationsToday()
        {
            return _historyTable.CreateQuery<NotificationHistory>()
                .Where(h => h.PartitionKey == NzTodayDateString)
                .ToList();
        }
        
        private bool CurrentPriceUnderThreshold(NotificationHistory notificationHistory, double currentPrice, MonitoredStock monitoredStock)
        {
            return notificationHistory == null
                ? currentPrice < monitoredStock.AlertPriceThreshold
                : currentPrice < notificationHistory.LastNotifiedPrice;
        }

        private IEnumerable<(MonitoredStock MonitoredStock, QuoteResult Quote, NotificationHistory History)> JoinStockDetails(
                IEnumerable<MonitoredStock> regionalStockGroup,
                IEnumerable<QuoteResult> stockQuotes,
                IEnumerable<NotificationHistory> allNotificationsToday)
        {
            var monitoredStocksWithQuotes = regionalStockGroup.Join(stockQuotes,
                s => s.Symbol, q => q.Symbol,
                (stock, quote) => (stock, quote));

            return monitoredStocksWithQuotes.GroupJoin(allNotificationsToday,
                join => join.stock?.Symbol,
                h => h.StockSymbol,
                (join, h) => (join.stock, join.quote, h.FirstOrDefault()));
        }

        private IEnumerable<IGrouping<string, MonitoredStock>> GroupStocksByRegion(IEnumerable<MonitoredStock> stocksToMonitor)
        {
            return stocksToMonitor.GroupBy(s => s.Region);
        }

        private IEnumerable<MonitoredStock> GetStocksToMonitor()
        {
            return _monitoredStocksTable.CreateQuery<MonitoredStock>().Where(s => s.IsActive).AsEnumerable();
        }

        private async Task<IEnumerable<QuoteResult>> GetStockQuotes(IGrouping<string, MonitoredStock> regionalStockGroup)
        {
            var stockRegion = regionalStockGroup.Key;
            var stockSymbols = regionalStockGroup.Select(s => s.Symbol);
            var quotes = await _yahooFinanceClient.GetQuotes(stockRegion, stockSymbols);
            return quotes.QuoteResponse.Result;
        }

        private async Task TriggerAlert(MonitoredStock stock, double currentPrice, string fullName)
        {
            await NotifyPriceDrop(stock.Symbol, currentPrice, fullName);
            await SaveNotificationHistory(stock.Symbol, currentPrice);
        }

        private async Task NotifyPriceDrop(string symbol, double currentPrice, string fullName)
        {
            var escapedSymbol = symbol.Replace(".", "·"); // Prevent hyperlink in Slack
            var message = $"{fullName} ({escapedSymbol})\n${currentPrice}";
            await _slackClient.SendMessageViaWebhook(_appSettings.NotificationsSlackWebhook, "Stock Alerts", message);
        }

        private async Task SaveNotificationHistory(string symbol, double currentPrice)
        {
            var entity = new NotificationHistory(NzTodayDateString, symbol) { LastNotifiedPrice = currentPrice };
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            await _historyTable.ExecuteAsync(insertOrMergeOperation);
        }

        private CloudTableClient CreateTableClient()
        {
            var storageAccount = CloudStorageAccount.Parse(_appSettings.StorageConnectionString);
            return storageAccount.CreateCloudTableClient();
        }
    }
}
