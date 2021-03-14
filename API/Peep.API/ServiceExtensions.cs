using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.Extensions.Configuration;
using Peep.Core.API.Providers;

namespace Peep.API
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddLogger(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration);

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
