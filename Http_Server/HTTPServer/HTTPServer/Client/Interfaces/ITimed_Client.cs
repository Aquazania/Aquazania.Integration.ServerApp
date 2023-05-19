namespace HTTPServer.Client
{
    public interface ITimed_Client
    {
        Task<HttpResponseMessage> SendAsync(object data, string url);
    }
    
}
