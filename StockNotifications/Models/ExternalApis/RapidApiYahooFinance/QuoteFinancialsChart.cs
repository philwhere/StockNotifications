namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteFinancialsChart
    {
        public QuoteYearly[] Yearly { get; set; }
        public QuoteFinancialsChartQuarterly[] Quarterly { get; set; }
    }
}