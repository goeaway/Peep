using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var options = new ConfigurationOptions
            {
                
            };
            var redis = ConnectionMultiplexer.Connect(options);
            return services.AddSingleton<IConnectionMultiplexer>(redis);
        }
    }
}
