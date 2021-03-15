using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using NpgsqlTypes;
using Peep.Core.Infrastructure;
using Peep.Crawler.Application.Options;
using Peep.Factories;
using Serilog.Sinks.PostgreSQL;

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

        public static IServiceCollection AddCrawler(this IServiceCollection services, CrawlConfigOptions options)
        {
            services.AddTransient<ICrawler, DistributedCrawler>(provider =>
            {
                var crawlerOptions = new CrawlerOptions
                {
                    BrowserAdapterFactory = new PuppeteerSharpBrowserAdapterFactory(options.BrowserPagesCount)
                };
                return new DistributedCrawler(crawlerOptions);
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
