using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using NpgsqlTypes;
using Peep.Core.API.Providers;
using Serilog.Sinks.MySQL;

namespace Peep.API
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            const string CONSOLE_OUTPUT_TEMPLATE = 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {JobId} {Message:lj}{NewLine}{Exception}";

            var loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Context", "API")
                .WriteTo.Console(outputTemplate: CONSOLE_OUTPUT_TEMPLATE)
                .WriteTo.MySQL(
                    configuration.GetConnectionString("db"),
                    configuration.GetSection("Logging")["TableName"]);

            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());
            return services;
        }

        public static IServiceCollection AddNowProvider(this IServiceCollection services)
        {
            return services.AddTransient<INowProvider, NowProvider>();
        }

        public static IServiceCollection AddCrawlCancellationTokenProvider(this IServiceCollection services)
        {
            services.AddSingleton<ICrawlCancellationTokenProvider>(new CrawlCancellationTokenProvider());
            return services;
        }
    }
}
