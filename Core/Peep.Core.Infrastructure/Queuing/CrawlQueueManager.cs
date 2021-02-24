using Peep.Core.API.Options;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Queuing
{
    public class CrawlQueueManager : ICrawlQueueManager
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly CachingOptions _cachingOptions;

        private const int DATABASE_ID = 2;
        private const string QUEUE_KEY = "crawlqueue";

        public CrawlQueueManager(
            IConnectionMultiplexer connection,
            CachingOptions cachingOptions)
        {
            _connection = connection;
            _cachingOptions = cachingOptions;
        }

        public async Task Enqueue(IEnumerable<Uri> uris)
        {
            await _connection
                .GetDatabase(DATABASE_ID)
                .ListRightPushAsync(
                    QUEUE_KEY, 
                    uris
                        .Select(u => new RedisValue(u.AbsoluteUri))
                        .ToArray()
                );
        }

        public async Task Clear()
        {
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            await server.FlushDatabaseAsync(DATABASE_ID);
        }
    }
}
