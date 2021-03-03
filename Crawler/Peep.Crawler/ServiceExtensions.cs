using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.Crawler.Application.Options;

namespace Peep.Crawler
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services)
        {
            const string OUTPUT_TEMPLATE = 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {JobId} {Message:lj}{NewLine}{Exception}";

            var loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: OUTPUT_TEMPLATE)
                .WriteTo
                    .File(
                        Path.Combine(AppContext.BaseDirectory, "log.txt"),
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: OUTPUT_TEMPLATE);

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

        public static IServiceCollection AddCrawlerOptions(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = new CrawlConfigOptions();
            configuration.GetSection(CrawlConfigOptions.Key)
                .Bind(options);
            return services.AddSingleton(options);
        }
    }
}
