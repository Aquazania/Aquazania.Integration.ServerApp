﻿using Aquazania.Integration.ServerApp.Client.Interfaces;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace HTTPServer.Client
{
    internal class HttpClientWrapper : ITimed_Client
    {
        private readonly HttpClient _httpClient;
        public HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }
        public async Task<HttpResponseMessage> SendAsync(object data, string url)
        {
            string payload = JsonConvert.SerializeObject(data, Formatting.Indented);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            return await _httpClient.SendAsync(message).ConfigureAwait(false);
        }
    }
}
