using Microsoft.Extensions.DependencyInjection;
using Peep.API.Application.Providers;
using Peep.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

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

        public static IServiceCollection AddCrawlCancellationTokenProvider(this IServiceCollection services)
        {
            services.AddSingleton<ICrawlCancellationTokenProvider>(new CrawlCancellationTokenProvider());
            return services;
        }
    }
}
