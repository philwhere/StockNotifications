namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteEarningsChartQuarterly
    {
        public string Date { get; set; }
        public double Actual { get; set; }
        public double Estimate { get; set; }
    }
}