namespace StockNotifications.Models.ExternalApis.RapidApiYahooFinance
{
    public class QuoteEarningsChart
    {
        public QuoteEarningsChartQuarterly[] Quarterly { get; set; }
        public double CurrentQuarterEstimate { get; set; }
        public string CurrentQuarterEstimateDate { get; set; }
        public int CurrentQuarterEstimateYear { get; set; }
        public int[] EarningsDate { get; set; }
    }
}