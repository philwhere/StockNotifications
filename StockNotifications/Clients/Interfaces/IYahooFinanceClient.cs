using System.Collections.Generic;
using System.Threading.Tasks;
using StockNotifications.Models.ExternalApis.RapidApiYahooFinance;

namespace StockNotifications.Clients.Interfaces
{
    public interface IYahooFinanceClient
    {
        Task<GetQuotesResponse> GetQuotes(string stockRegion, IEnumerable<string> stockSymbols);
    }
}