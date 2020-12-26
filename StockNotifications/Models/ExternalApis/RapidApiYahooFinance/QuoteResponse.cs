namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteResponse
    {
        public QuoteResult[] Result { get; set; }
        public object Error { get; set; }
    }
}