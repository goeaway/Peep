using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Brighter;
using Paramore.Brighter.Extensions.DependencyInjection;
using Paramore.Brighter.MessagingGateway.RMQ;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Queuing;
using Peep.Core.Infrastructure.Subscriptions;
using Peep.Crawler.Subscriptions;
using Peep.Filtering;
using Peep.Queueing;

namespace Peep.Crawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogger();
                    services.AddCrawler();
                    services.AddCrawlerOptions(hostContext.Configuration);
                    services.AddHostedService<Worker>();

                    services.AddRedis(hostContext.Configuration);

                    services.AddTransient<ICrawlFilter, CacheCrawlFilter>();
                    services.AddTransient<ICrawlQueue, CacheCrawlQueue>();
                    services.AddTransient<ICrawlDataSink, CacheCrawlDataSink>();

                    services.AddSingleton<ICrawlCancellationTokenProvider, CrawlCancellationTokenProvider>();

                    // in memory queue for jobs
                    services.AddSingleton<IJobQueue, JobQueue>();

                    // message subscribers

                    services.AddMessagingOptions(hostContext.Configuration, out var messagingOptions);

                    services.AddBrighter(options => {
                        var messageStore = new InMemoryMessageStore();
                        var rmq = new RmqMessageProducer(new RmqMessagingGatewayConnection
                        {
                            AmpqUri = new AmqpUriSpecification(
                                new Uri($"amqp://guest:guest@{messagingOptions.Hostname}:{messagingOptions.Port}")),
                            Exchange = new Exchange("crawler")
                        });

                        options.BrighterMessaging = new BrighterMessaging(messageStore, rmq);
                    });
                });
    }
}
