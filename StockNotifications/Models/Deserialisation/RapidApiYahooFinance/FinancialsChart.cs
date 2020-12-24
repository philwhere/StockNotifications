namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class FinancialsChart
    {
        public Yearly[] yearly { get; set; }
        public FinancialsChartQuarterly[] quarterly { get; set; }
    }
}