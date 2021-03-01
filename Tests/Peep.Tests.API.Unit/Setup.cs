using System;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Peep.API;
using Peep.API.Persistence;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;

namespace Peep.Tests.API.Unit
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
            public MessagingOptions MessagingOptions { get; set; }
            public ICrawlCancellationTokenProvider TokenProvider { get; set; }
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
                    if(options.MessagingOptions != null)
                    {
                        services.AddSingleton(options.MessagingOptions);
                    }

                    if (options.TokenProvider != null)
                    {
                        services.AddSingleton(options.TokenProvider);
                    }
                }));
            var client = server.CreateClient();
            return (server, client);
        }

        public static (TestServer, HttpClient, PeepApiContext) CreateDataBackedServer(CreateServerOptions options = null)
        {
            options ??= new CreateServerOptions();

            var context = CreateContext();

            var server = new TestServer(new WebHostBuilder()
                .UseStartup<Startup>()
                .ConfigureTestServices(services =>
                {
                    services.AddSingleton(context);

                    if(options.TokenProvider != null)
                    {
                        services.AddSingleton(options.TokenProvider);
                    }
                }));
            var client = server.CreateClient();
            return (server, client, context);
        }


    }
}
