namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteResponse
    {
        public Result[] result { get; set; }
        public object error { get; set; }
    }
}