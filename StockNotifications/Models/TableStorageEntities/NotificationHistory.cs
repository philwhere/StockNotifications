using System;
using Microsoft.Azure.Cosmos.Table;

namespace StockNotifications.Models.TableStorageEntities
{
    public class NotificationHistory : TableEntity
    {
        public NotificationHistory()
        { }

        public NotificationHistory(string dateNz, string stockSymbol)
        {
            PartitionKey = dateNz;
            RowKey = stockSymbol;
        }

        public double LastNotifiedPrice { get; set; }
        public DateTime Date => DateTime.Parse(PartitionKey);
        public string StockSymbol => RowKey;
    }
}