namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class EarningsChartQuarterly
    {
        public string date { get; set; }
        public double actual { get; set; }
        public double estimate { get; set; }
    }
}