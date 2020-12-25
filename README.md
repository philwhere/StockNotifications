# StockNotifications
Slack notification for price drops

# local.settings.json Placed at the root of the functions project
```{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "StorageConnectionString": "UseDevelopmentStorage=true",
    "RapidApiKey": "Get yours here https://rapidapi.com/apidojo/api/Yahoo%20Finance",
    "RapidApiYahooFinanceHost": "apidojo-yahoo-finance-v1.p.rapidapi.com",
    "NotificationsSlackWebhook": "New webhooks https://{your workspace here}.slack.com/apps/new/A0F7XDUAZ-incoming-webhooks / Existing webhooks /apps/A0F7XDUAZ-incoming-webhooks",
    "WEBSITE_TIME_ZONE": "New Zealand Standard Time",
    "MonitorStocksSchedule": "An NCron schedule e.g. */10 * * * * * (every 10 secs for easy dev)"
  }
}
