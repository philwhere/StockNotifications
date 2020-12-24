using System;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using StockNotifications;
using StockNotifications.Clients;
using StockNotifications.Clients.Interfaces;

[assembly: FunctionsStartup(typeof(Startup))]

namespace StockNotifications
{
    public class Startup : FunctionsStartup
    {
        private IAsyncPolicy<HttpResponseMessage> RetryPolicy => GetRetryPolicy();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient<IYahooFinanceClient, RapidApiYahooFinanceClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy);
            builder.Services.AddHttpClient<ISlackClient, SlackClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(RetryPolicy);
        }


        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var jitterer = new Random();
            const int maxRetryCount = 3;
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(maxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))
                                                               + TimeSpan.FromMilliseconds(jitterer.Next(333, 666)));
        }
    }
}