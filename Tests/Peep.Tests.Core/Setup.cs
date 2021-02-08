using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Peep.API;
using System;
using System.Net.Http;

namespace Peep.Tests.Core
{
    public static class Setup
    {
        public class CreateServerOptions
        {
        }
        public static (TestServer, HttpClient) CreateServer(CreateServerOptions options = null)
        {
            if (options == null)
            {
                options = new CreateServerOptions();
            }

            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureTestServices(services =>
                {
                }));
            var client = server.CreateClient();
            return (server, client);
        }
    }
}
