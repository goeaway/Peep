﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.Crawler.Options;

namespace Peep.Crawler
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services)
        {
            var loggerConfig = new LoggerConfiguration()
                        .WriteTo
                            .Console()
                        .WriteTo
                            .File(
                                Path.Combine(AppContext.BaseDirectory, "log.txt"),
                                rollingInterval: RollingInterval.Day);

            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());
            return services;
        }

        public static IServiceCollection AddCrawler(this IServiceCollection services)
        {
            services.AddTransient<ICrawler, DistributedCrawler>();
            return services;
        }

        public static IServiceCollection AddNowProvider(this IServiceCollection services)
        {
            return services.AddTransient<INowProvider, NowProvider>();
        }

        public static IServiceCollection AddMessagingOptions(
            this IServiceCollection services,
            IConfiguration configuration,
            out MessagingOptions messagingOptions)
        {
            messagingOptions = new MessagingOptions();
            configuration.GetSection(MessagingOptions.Key).Bind(messagingOptions);

            return services.AddSingleton(messagingOptions);
        }

        public static IServiceCollection AddCrawlerOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = new CrawlConfigOptions();
            configuration.GetSection(CrawlConfigOptions.Key).Bind(options);
            return services.AddSingleton(options);
        }
    }
}
