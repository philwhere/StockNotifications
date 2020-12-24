using Microsoft.Azure.Cosmos.Table;

namespace StockNotifications.Models.TableStorageEntities
{
    public class MonitoredStock : TableEntity
    {
        public bool IsActive { get; set; }
        public double AlertPriceThreshold { get; set; }
        public string Region { get; set; }
        public string FullExchangeName => PartitionKey;
        public string Symbol => RowKey;
    }
}