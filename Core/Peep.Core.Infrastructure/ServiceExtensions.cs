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
            var redis = ConnectionMultiplexer.Connect("172.22.128.1:6379");
            return services.AddSingleton<IConnectionMultiplexer>(redis);
        }
    }
}
