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
            foreach (var stock in stocksToMonitor)
            {
                var (currentPrice, fullName) = await GetStockQuoteDetails(stock.Region, stock.Symbol);
                var notificationHistory = GetNotificationHistoryForToday(stock.Symbol);
                if (notificationHistory == null)
                {
                    if (currentPrice < stock.AlertPriceThreshold)
                        await TriggerAlert(stock, currentPrice, fullName);
                }
                else if (currentPrice < notificationHistory.LastNotifiedPrice)
                    await TriggerAlert(stock, currentPrice, fullName);
            }
        }


        private IEnumerable<MonitoredStock> GetStocksToMonitor()
        {
            var table = _tableClient.GetTableReference(StocksToMonitorTableName);
            var stocks = table.CreateQuery<MonitoredStock>().Where(s => s.IsActive).ToList();
            return stocks;
        }

        private async Task<(double currentPrice, string fullName)> GetStockQuoteDetails(string stockRegion, string stockSymbol)
        {
            var quote = await _yahooFinanceClient.GetQuotes(stockRegion, stockSymbol);
            var stockResult = quote.quoteResponse.result.First(r => r.symbol == stockSymbol);
            return (stockResult.regularMarketPrice, stockResult.longName);
        }


        private NotificationHistory GetNotificationHistoryForToday(string stockStockSymbol)
        {
            var table = _tableClient.GetTableReference(HistoryTableName);
            var history = table.CreateQuery<NotificationHistory>()
                .Where(h => h.PartitionKey == NzTodayDateString && 
                            h.RowKey == stockStockSymbol)
                .ToList();
            return history.FirstOrDefault();
        }

        private async Task TriggerAlert(MonitoredStock stock, double currentPrice, string shortName)
        {
            await NotifyPriceDrop(stock.Symbol, currentPrice, shortName);
            await SaveNotificationHistory(stock.Symbol, currentPrice);
        }

        private async Task NotifyPriceDrop(string stockSymbol, double currentPrice, string stockFullName)
        {
            var escapedSymbol = stockSymbol.Replace(".", "·");
            var message = $"{stockFullName} ({escapedSymbol}) ${currentPrice}";
            await _slackClient.SendMessageViaWebhook(_appSettings.NotificationsSlackWebhook, "Stock Alerts", message);
        }

        private async Task SaveNotificationHistory(string stockSymbol, double currentPrice)
        {
            var entity = new NotificationHistory(NzTodayDateString, stockSymbol) { LastNotifiedPrice = currentPrice };
            var insertOrMergeOperation = TableOperation.InsertOrMerge(entity);
            var table = _tableClient.GetTableReference(HistoryTableName);
            await table.ExecuteAsync(insertOrMergeOperation);
        }
    }
}
