using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Peep.API.Application.Options;
using Peep.API.Application.Providers;
using Serilog;
using System;
using System.IO;

namespace Peep.API
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(this IServiceCollection services)
        {
            var loggerConfig = new LoggerConfiguration()
                        .WriteTo
                            .File(
                                Path.Combine(AppContext.BaseDirectory, "log.txt"),
                                rollingInterval: RollingInterval.Day);

            services.AddSingleton<ILogger>(loggerConfig.CreateLogger());
            return services;
        }

        public static IServiceCollection AddCrawler(this IServiceCollection services)
        {
            services.AddTransient<ICrawler, Crawler>();
            return services;
        }

        public static IServiceCollection AddNowProvider(this IServiceCollection services)
        {
            return services.AddTransient<INowProvider, NowProvider>();
        }

        public static IServiceCollection AddRunningCrawlJobProvider(this IServiceCollection services)
        {
            return services.AddTransient<IRunningCrawlJobProvider, RunningCrawlJobProvider>();
        }

        public static IServiceCollection AddCrawlCancellationTokenProvider(this IServiceCollection services)
        {
            services.AddSingleton<ICrawlCancellationTokenProvider>(new CrawlCancellationTokenProvider());
            return services;
        }

        public static IServiceCollection AddCrawlConfigOptions(this IServiceCollection services, IConfiguration configuration)
        {
            var options = new CrawlConfigOptions();
            configuration.GetSection(CrawlConfigOptions.Key).Bind(options);
            return services.AddSingleton(options);
        }
    }
}
