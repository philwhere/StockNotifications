namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class EarningsChart
    {
        public EarningsChartQuarterly[] quarterly { get; set; }
        public double currentQuarterEstimate { get; set; }
        public string currentQuarterEstimateDate { get; set; }
        public int currentQuarterEstimateYear { get; set; }
        public int[] earningsDate { get; set; }
    }
}