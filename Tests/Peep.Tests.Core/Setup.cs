using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Peep.API;
using Peep.API.Persistence;
using System;
using System.Net.Http;

namespace Peep.Tests.Core
{
    public static class Setup
    {
        public static PeepApiContext CreateContext(string databaseName = null)
        {
            var options = new DbContextOptionsBuilder<PeepApiContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;
            return new PeepApiContext(options);
        }


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
