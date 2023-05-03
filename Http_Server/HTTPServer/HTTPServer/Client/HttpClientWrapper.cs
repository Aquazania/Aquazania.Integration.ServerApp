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
        }
        public async Task<HttpResponseMessage> SendAsync(object data, string url)
        {
            string payload = JsonConvert.SerializeObject(data, Formatting.Indented);
            //string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //File.WriteAllText(Path.Combine(docPath, "SendingAttempt.txt"), payload);
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            return await _httpClient.SendAsync(message).ConfigureAwait(false);
        }

    }
}
