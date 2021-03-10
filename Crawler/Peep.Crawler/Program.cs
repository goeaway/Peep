using System;
using System.Reflection;
using MassTransit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peep.Core.API;
using Peep.Core.API.Behaviours;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Queuing;
using Peep.Crawler.Messages;
using Peep.Filtering;
using Peep.Queueing;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Serilog;

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
                    services.AddLogger(hostContext.Configuration);
                    services.AddCrawler();
                    services.AddCrawlerOptions(hostContext.Configuration);
                    services.AddMessagingOptions(hostContext.Configuration, out var messagingOptions);
                    services.AddCachingOptions(hostContext.Configuration, out var cachingOptions);
                    services.AddHostedService(provider =>
                    {
                        var scope = provider.CreateScope();

                        var logger = scope.ServiceProvider.GetRequiredService<ILogger>();
                        var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                        
                        return new Worker(
                            logger,
                            jobQueue,
                            mediator,
                            publishEndpoint
                        );
                    });

                    services.AddRedis(cachingOptions);
                    
                    services.AddMediatR(Assembly.GetAssembly(typeof(RunCrawlRequest)));
                    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

                    services.AddTransient<ICrawlFilter, CacheCrawlFilter>();
                    services.AddTransient<ICrawlQueue, CacheCrawlQueue>();

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

                            cfg.ReceiveEndpoint(
                                "crawl-queued-" + Environment.MachineName, 
                                e =>
                                {
                                    e.Consumer(
                                        () => new CrawlQueuedConsumer(ctx.GetRequiredService<IMediator>())
                                    );
                                });

                            cfg.ReceiveEndpoint(
                                "crawl-cancelled-" + Environment.MachineName,
                                e =>
                                {
                                    e.Consumer(
                                        () => new CrawlCancelledConsumer(ctx.GetRequiredService<IMediator>())
                                    );
                                });
                        });
                    });
                });
    }
}
