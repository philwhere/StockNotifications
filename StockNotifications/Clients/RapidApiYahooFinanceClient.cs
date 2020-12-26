using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StockNotifications.Clients.Interfaces;
using StockNotifications.Models.ExternalApis.RapidApiYahooFinance;

namespace StockNotifications.Clients
{
    public class RapidApiYahooFinanceClient : IYahooFinanceClient
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpClient;

        public RapidApiYahooFinanceClient(HttpClient httpClient, IOptions<AppSettings> appSettings)
        {
            _httpClient = httpClient;
            _appSettings = appSettings.Value;
        }

        public async Task<GetQuotesResponse> GetQuotes(string stockRegion, IEnumerable<string> stockSymbols)
        {
            var queryParams = GetQuotesQueryParams(stockRegion, stockSymbols);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri =
                    new Uri($"https://{_appSettings.RapidApiYahooFinanceHost}/market/v2/get-quotes?{queryParams}"),
                Headers =
                {
                    {"x-rapidapi-key", _appSettings.RapidApiKey},
                    {"x-rapidapi-host", _appSettings.RapidApiYahooFinanceHost}
                }
            };
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsAsync<GetQuotesResponse>();
            return body;
        }


        private string GetQuotesQueryParams(string stockRegion, IEnumerable<string> stockSymbols)
        {
            var symbols = string.Join(',', stockSymbols);
            return $"region={stockRegion}&symbols={symbols}";
        }
    }
}