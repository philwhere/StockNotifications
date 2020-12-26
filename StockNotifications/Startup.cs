using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public Startup()
        {
            _configuration = BuildConfiguration();
        }

        private IAsyncPolicy<HttpResponseMessage> RetryPolicy => GetRetryPolicy();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.Configure<AppSettings>(_configuration);

            builder.Services.AddHttpClient<IYahooFinanceClient, RapidApiYahooFinanceClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(15))
                .AddPolicyHandler(RetryPolicy);

            builder.Services.AddHttpClient<ISlackClient, SlackClient>()
                .SetHandlerLifetime(TimeSpan.FromMinutes(15))
                .AddPolicyHandler(RetryPolicy);
        }


        private IConfigurationRoot BuildConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();
        }

        private IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var jitterer = new Random();
            const int maxRetryCount = 3;
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(maxRetryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))
                                                                  + TimeSpan.FromMilliseconds(jitterer.Next(333, 666)));
        }
    }
}