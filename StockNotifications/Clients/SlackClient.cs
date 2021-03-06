﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using StockNotifications.Clients.Interfaces;

namespace StockNotifications.Clients
{
    public class SlackClient : ISlackClient
    {
        private readonly HttpClient _httpClient;

        public SlackClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendMessageViaWebhook(string webhookUrl, string senderName, string messageText)
        {
            var messageContent = CreatePayload(senderName, messageText);
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(webhookUrl),
                Content = messageContent
            };
            await _httpClient.SendAsync(request);
        }


        private HttpContent CreatePayload(string senderName, string messageText)
        {
            var message = new
            {
                username = senderName,
                text = messageText
            };
            return new StringContent(JsonSerializer.Serialize(message));
        }
    }
}