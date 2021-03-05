using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using NpgsqlTypes;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.Crawler.Application.Options;
using Serilog.Sinks.PostgreSQL;

namespace Peep.Crawler
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services, IConfiguration configuration)
        {
            const string CONSOLE_OUTPUT_TEMPLATE = 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {JobId} {Message:lj}{NewLine}{Exception}";

            var loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Context", Environment.MachineName)
                .WriteTo.Console(outputTemplate: CONSOLE_OUTPUT_TEMPLATE)
                .WriteTo.MySQL(
                    configuration.GetConnectionString("db"),
                    configuration.GetSection("Logging")["TableName"]);

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
