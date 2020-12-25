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
        private const string StocksToMonitorTableName = "StocksToMonitor";
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
        public async Task Run([TimerTrigger("0 30 9-17 * * *")] TimerInfo myTimer, ILogger log)
        {

            var stocksToMonitor = GetStocksToMonitor();
            var regionalStockGroups = GetStocksByRegion(stocksToMonitor);

            foreach (var regionalStocks in regionalStockGroups)
            {
                var stockQuotes = await GetStockQuotes(regionalStocks);
                foreach (var (currentPrice, name, symbol) in stockQuotes)
                {
                    var monitoredStock = regionalStocks.Single(s => s.Symbol == symbol);
                    var notificationHistory = GetNotificationHistoryForToday(symbol);
                    if (notificationHistory == null)
                    {
                        if (currentPrice < monitoredStock.AlertPriceThreshold)
                            await TriggerAlert(monitoredStock, currentPrice, name);
                    }
                    else if (currentPrice < notificationHistory.LastNotifiedPrice)
                        await TriggerAlert(monitoredStock, currentPrice, name);
                }
            }
        }


        private static IEnumerable<IGrouping<string, MonitoredStock>> GetStocksByRegion(
            IReadOnlyList<MonitoredStock> stocksToMonitor)
        {
            return stocksToMonitor.GroupBy(s => s.Region);
        }

        private IReadOnlyList<MonitoredStock> GetStocksToMonitor()
        {
            var table = _tableClient.GetTableReference(StocksToMonitorTableName);
            var stocks = table.CreateQuery<MonitoredStock>().Where(s => s.IsActive).ToList();
            return stocks;
        }

        private async Task<IEnumerable<(double RegularMarketPrice, string LongName, string Symbol)>> GetStockQuotes(
            IGrouping<string, MonitoredStock> regionalStockGroups)
        {
            var stockRegion = regionalStockGroups.Key;
            var stockSymbols = regionalStockGroups.Select(s => s.Symbol);
            var quotes = await _yahooFinanceClient.GetQuotes(stockRegion, stockSymbols);
            return quotes.quoteResponse.result.Select(r => (r.regularMarketPrice, r.longName, r.symbol));
        }

        private NotificationHistory GetNotificationHistoryForToday(string symbol)
        {
            var table = _tableClient.GetTableReference(HistoryTableName);
            var history = table.CreateQuery<NotificationHistory>()
                .Where(h => h.PartitionKey == NzTodayDateString &&
                            h.RowKey == symbol)
                .ToList();
            return history.FirstOrDefault();
        }

        private async Task TriggerAlert(MonitoredStock stock, double currentPrice, string fullName)
        {
            await NotifyPriceDrop(stock.Symbol, currentPrice, fullName);
            await SaveNotificationHistory(stock.Symbol, currentPrice);
        }

        private async Task NotifyPriceDrop(string symbol, double currentPrice, string fullName)
        {
            var escapedSymbol = symbol.Replace(".", "·");
            var message = $"{fullName} ({escapedSymbol}) ${currentPrice}";
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
