using Microsoft.Extensions.Configuration;

namespace StockNotifications
{
    public class AppSettings
    {
        public string StorageConnectionString { get; set; }
        public string RapidApiKey { get; set; }
        public string RapidApiYahooFinanceHost { get; set; }
        public string NotificationsSlackWebhook { get; set; }


        public static AppSettings LoadAppSettings()
        {
            var configRoot = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();
            var appSettings = configRoot.Get<AppSettings>();
            return appSettings;
        }
    }
}
