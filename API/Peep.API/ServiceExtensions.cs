using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.API.Application.Options;

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

        public static IServiceCollection AddNowProvider(this IServiceCollection services)
        {
            return services.AddTransient<INowProvider, NowProvider>();
        }

        public static IServiceCollection AddCrawlCancellationTokenProvider(this IServiceCollection services)
        {
            services.AddSingleton<ICrawlCancellationTokenProvider>(new CrawlCancellationTokenProvider());
            return services;
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
    }
}
