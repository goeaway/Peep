using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Peep.BrowserAdapter;
using Serilog;
using Peep.Core.Infrastructure;
using Peep.Crawler.Application.Options;

namespace Peep.Crawler
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(
            this IServiceCollection services, 
            IConfiguration configuration, 
            CrawlerId crawlerId)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("Context", crawlerId);

            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());
            return services;
        }

        public static IServiceCollection AddBrowserAdapter(this IServiceCollection services, CrawlConfigOptions options)
        {
            return services.AddSingleton<IBrowserAdapter, PlaywrightSharpBrowserAdapter>(
                provider =>
                {
                    try
                    {
                        return new PlaywrightSharpBrowserAdapter(options.BrowserPagesCount);
                    }
                    catch (Exception e)
                    {
                        provider.GetRequiredService<ILogger>()
                            .Fatal(e, "Error occurred trying to create browser adapter");
                        throw;
                    }
                });
        }

        public static IServiceCollection AddCrawler(this IServiceCollection services)
        {
            services.AddTransient<ICrawler, DistributedCrawler>(provider =>
            {
                var browserAdapter = provider.GetRequiredService<IBrowserAdapter>();
                return new DistributedCrawler(browserAdapter);
            });
            return services;
        }

        public static IServiceCollection AddCrawlerOptions(
            this IServiceCollection services,
            IConfiguration configuration,
            out CrawlConfigOptions options)
        {
            options = new CrawlConfigOptions();
            configuration.GetSection(CrawlConfigOptions.Key)
                .Bind(options);
            return services.AddSingleton(options);
        }
    }
}
