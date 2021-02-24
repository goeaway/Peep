using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peep.Core.API;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Queuing;
using Peep.Crawler.Messages;
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
                    services.AddMessagingOptions(hostContext.Configuration, out var messagingOptions);
                    services.AddCachingOptions(hostContext.Configuration, out var cachingOptions);
                    services.AddHostedService<Worker>();

                    services.AddRedis(cachingOptions);

                    services.AddTransient<ICrawlFilter, CacheCrawlFilter>();
                    services.AddTransient<ICrawlQueue, CacheCrawlQueue>();
                    services.AddTransient<ICrawlDataSink, CacheCrawlDataSink>();

                    services.AddSingleton<ICrawlCancellationTokenProvider, CrawlCancellationTokenProvider>();

                    // in memory queue for jobs
                    services.AddSingleton<IJobQueue, JobQueue>();

                    services.AddMassTransitHostedService();
                    services.AddMassTransit(options =>
                    {
                        options.AddConsumer<CrawlQueuedConsumer>();
                        options.AddConsumer<CrawlCancelledConsumer>();

                        options.UsingRabbitMq((ctx, cfg) =>
                        {
                            cfg.Host(messagingOptions.Hostname, "/", h =>
                            {
                                h.Username(messagingOptions.Username);
                                h.Password(messagingOptions.Password);
                            });

                            cfg.ConfigureEndpoints(ctx);
                        });
                    });
                });
    }
}
