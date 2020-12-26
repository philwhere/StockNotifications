namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteEarnings
    {
        public int MaxAge { get; set; }
        public QuoteEarningsChart QuoteEarningsChart { get; set; }
        public QuoteFinancialsChart QuoteFinancialsChart { get; set; }
        public string FinancialCurrency { get; set; }
    }
}