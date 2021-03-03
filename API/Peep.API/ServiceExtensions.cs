using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using Peep.Core;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;

namespace Peep.API
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
