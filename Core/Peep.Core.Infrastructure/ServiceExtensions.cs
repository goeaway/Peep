using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Peep.Core.API.Options;

namespace Peep.Core.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, CachingOptions cachingOptions)
        {
            var redis = ConnectionMultiplexer.Connect($"{cachingOptions.Hostname}:{cachingOptions.Port},allowAdmin=true");
            return services.AddSingleton<IConnectionMultiplexer>(redis);
        }
    }
}
