using Peep.Core.API.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Filtering
{
    public class CrawlFilterManager : ICrawlFilterManager
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly CachingOptions _cachingOptions;

        private const int DATABASE_ID = 1;

        public CrawlFilterManager(
            IConnectionMultiplexer connection,
            CachingOptions cachingOptions)
        {
            _connection = connection;
            _cachingOptions = cachingOptions;
        }

        public async Task Clear()
        {
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            await server.FlushDatabaseAsync(DATABASE_ID);
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(_connection
                .GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}")
                .Keys(DATABASE_ID)
                .Count());
        }
    }
}
