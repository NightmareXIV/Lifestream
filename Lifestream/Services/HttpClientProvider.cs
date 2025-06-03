using System.Net.Http;

namespace Lifestream.Services;
public class HttpClientProvider : IDisposable
{
    private HttpClient Client;
    private HttpClientProvider()
    {
    }

    public void Dispose()
    {
        Client?.Dispose();
    }

    public HttpClient Get()
    {
        Client ??= new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        return Client;
    }
}
