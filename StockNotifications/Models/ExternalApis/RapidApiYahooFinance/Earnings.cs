namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class Earnings
    {
        public int maxAge { get; set; }
        public EarningsChart earningsChart { get; set; }
        public FinancialsChart financialsChart { get; set; }
        public string financialCurrency { get; set; }
    }
}