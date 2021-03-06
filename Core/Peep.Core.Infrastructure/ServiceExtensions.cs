using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Peep.Core.API.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;

namespace Peep.Core.Infrastructure
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, CachingOptions cachingOptions)
        {
            var redis = ConnectionMultiplexer.Connect($"{cachingOptions.Hostname}:{cachingOptions.Port},allowAdmin=true");
            var redisLockFactory = RedLockFactory.Create(new List<RedLockMultiplexer>
            {
                redis
            });
            
            return services
                .AddSingleton<IConnectionMultiplexer>(redis)
                .AddSingleton<IDistributedLockFactory>(redisLockFactory);
        }
    }
}
