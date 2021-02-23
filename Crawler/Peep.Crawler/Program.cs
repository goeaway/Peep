using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                    services.AddHostedService<Worker>();

                    services.AddRedis(hostContext.Configuration);

                    services.AddTransient<ICrawlFilter, CacheCrawlFilter>();
                    services.AddTransient<ICrawlQueue, CacheCrawlQueue>();
                    services.AddTransient<ICrawlDataSink, CacheCrawlDataSink>();

                    services.AddSingleton<ICrawlCancellationTokenProvider, CrawlCancellationTokenProvider>();

                    // in memory queue for jobs
                    services.AddSingleton<IJobQueue, JobQueue>();

                    services.AddMessagingOptions(hostContext.Configuration, out var messagingOptions);

                    services.AddMassTransit(options =>
                    {
                        options.UsingRabbitMq((ctx, cfg) =>
                        {
                            cfg.Host("172.22.128.1", "/", h =>
                            {
                                h.Username("guest");
                                h.Password("guest");
                            });

                            cfg.ReceiveEndpoint("crawl-queued-listener", e =>
                            {
                                e.Consumer<CrawlQueuedConsumer>(
                                    () => new CrawlQueuedConsumer(ctx.GetRequiredService<IJobQueue>()));
                            });

                            cfg.ReceiveEndpoint("crawl-cancelled-listener", e =>
                            {
                                e.Consumer<CrawlCancelledConsumer>(
                                    () => new CrawlCancelledConsumer(
                                        ctx.GetRequiredService<IJobQueue>(),
                                        ctx.GetRequiredService<ICrawlCancellationTokenProvider>()));
                            });
                        });
                    });
                });
    }
}
