using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using StockNotifications.Clients.Interfaces;
using StockNotifications.Extensions;
using StockNotifications.Models.TableStorageEntities;

namespace StockNotifications
{
    public class MonitorStocks
    {
        private readonly IYahooFinanceClient _yahooFinanceClient;
        private readonly ISlackClient _slackClient;
        private readonly CloudTableClient _tableClient;
        private readonly AppSettings _appSettings;
        private const string HistoryTableName = "NotificationHistory";
        private const string MonitoredStocksTableName = "StocksToMonitor";
        private static string NzTodayDateString => DateTime.Now.ToTableDateFormat();

        public MonitorStocks(IYahooFinanceClient yahooFinanceClient, ISlackClient slackClient)
        {
            _yahooFinanceClient = yahooFinanceClient;
            _slackClient = slackClient;
            _appSettings = AppSettings.LoadAppSettings();
            var storageAccount = CloudStorageAccount.Parse(AppSettings.LoadAppSettings().StorageConnectionString);
            _tableClient = storageAccount.CreateCloudTableClient();
        }

        [FunctionName("MonitorStocks")]
        public async Task Run([TimerTrigger("%MonitorStocksSchedule%")] TimerInfo timer, ILogger log)
        {
            var stocksToMonitor = GetStocksToMonitor();
            var regionalStockGroups = GroupStocksByRegion(stocksToMonitor);

            foreach (var regionalStockGroup in regionalStockGroups)
            {
                var stockQuotes = await GetStockQuotes(regionalStockGroup);
                var monitoredStocksWithQuotes = JoinStockDetails(regionalStockGroup, stockQuotes);

                foreach (var (monitoredStock, quote) in monitoredStocksWithQuotes)
                {
                    var (currentPrice, name, symbol) = quote;
                    var notificationHistory = GetNotificationHistoryForToday(symbol);

                    if (CurrentPriceUnderThreshold(notificationHistory, currentPrice, monitoredStock))
                        await TriggerAlert(monitoredStock, currentPrice, name);
                }
            }
        }


        private bool CurrentPriceUnderThreshold(NotificationHistory notificationHistory, double currentPrice, MonitoredStock monitoredStock)
        {
            return notificationHistory == null
                ? currentPrice < monitoredStock.AlertPriceThreshold
                : currentPrice < notificationHistory.LastNotifiedPrice;
        }

        private IEnumerable<(MonitoredStock MonitoredStock, (double RegularMarketPrice, string LongName, string Symbol) Quote)>
            JoinStockDetails(IGrouping<string, MonitoredStock> regionalStockGroup,
                IEnumerable<(double RegularMarketPrice, string LongName, string Symbol)> stockQuotes)
        {
            return regionalStockGroup.AsEnumerable().Join(stockQuotes,
                s => s.Symbol, q => q.Symbol,
                (stock, quote) => (stock, quote));
        }

        private IEnumerable<IGrouping<string, MonitoredStock>> GroupStocksByRegion(IReadOnlyList<MonitoredStock> stocksToMonitor)
        {
            return stocksToMonitor.GroupBy(s => s.Region);
        }

        private IReadOnlyList<MonitoredStock> GetStocksToMonitor()
        {
            var table = _tableClient.GetTableReference(MonitoredStocksTableName);
            return table.CreateQuery<MonitoredStock>().Where(s => s.IsActive).ToList();
        }

        private async Task<IEnumerable<(double RegularMarketPrice, string LongName, string Symbol)>> GetStockQuotes(
            IGrouping<string, MonitoredStock> regionalStockGroup)
        {
            var stockRegion = regionalStockGroup.Key;
            var stockSymbols = regionalStockGroup.Select(s => s.Symbol);
            var quotes = await _yahooFinanceClient.GetQuotes(stockRegion, stockSymbols);
            return quotes.quoteResponse.result.Select(r => (r.regularMarketPrice, r.longName, r.symbol));
        }

        private NotificationHistory GetNotificationHistoryForToday(string symbol)
        {
            var table = _tableClient.GetTableReference(HistoryTableName);
            var history = table.CreateQuery<NotificationHistory>()
                .Where(h => h.PartitionKey == NzTodayDateString && h.RowKey == symbol)
                .AsEnumerable();
            return history.FirstOrDefault();
        }

        private async Task TriggerAlert(MonitoredStock stock, double currentPrice, string fullName)
        {
            await NotifyPriceDrop(stock.Symbol, currentPrice, fullName);
            await SaveNotificationHistory(stock.Symbol, currentPrice);
        }

        private async Task NotifyPriceDrop(string symbol, double currentPrice, string fullName)
        {
            var escapedSymbol = symbol.Replace(".", "�"); // Prevent hyperlink in Slack
            var message = $"{fullName} ({escapedSymbol})\n${currentPrice}";
            await _slackClient.SendMessageViaWebhook(_appSettings.NotificationsSlackWebhook, "Stock Alerts", message);
        }

        private async Task SaveNotificationHistory(string symbol, double currentPrice)
        {
            var entity = new NotificationHistory(NzTodayDateString, symbol) { LastNotifiedPrice = currentPrice };
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            var table = _tableClient.GetTableReference(HistoryTableName);
            await table.ExecuteAsync(insertOrMergeOperation);
        }
    }
}
