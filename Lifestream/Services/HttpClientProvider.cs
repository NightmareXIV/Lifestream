using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
