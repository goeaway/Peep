using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Peep.Core.API.Options;

namespace Peep.Core.API
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddMessagingOptions(
            this IServiceCollection services, 
            IConfiguration configuration,
            out MessagingOptions messagingOptions)
        {
            messagingOptions = new MessagingOptions();
            configuration
                .GetSection(MessagingOptions.Key)
                .Bind(messagingOptions);
            return services.AddSingleton(messagingOptions);
        }

        public static IServiceCollection AddCachingOptions(this IServiceCollection services,
            IConfiguration configuration,
            out CachingOptions cachingOptions)
        {
            cachingOptions = new CachingOptions();
            configuration.GetSection(CachingOptions.Key).Bind(cachingOptions);

            return services.AddSingleton(cachingOptions);
        }

        public static IServiceCollection AddMonitoringOptions(this IServiceCollection services,
            IConfiguration configuration,
            out MonitoringOptions monitoringOptions)
        {
            monitoringOptions = new MonitoringOptions();
            configuration.GetSection(MonitoringOptions.Key).Bind(monitoringOptions);
            return services.AddSingleton(monitoringOptions);
        }
    }
}