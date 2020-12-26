namespace StockNotifications
{
    public class AppSettings
    {
        public string StorageConnectionString { get; set; }
        public string RapidApiKey { get; set; }
        public string RapidApiYahooFinanceHost { get; set; }
        public string NotificationsSlackWebhook { get; set; }
    }
}
