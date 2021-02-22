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

        private const int DATABASE_ID = 2;
        private const string QUEUE_KEY = "crawlqueue";

        public CrawlQueueManager(
            IConnectionMultiplexer connection)
        {
            _connection = connection;
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
            var server = _connection.GetServer("localhost:6379");

            await server.FlushDatabaseAsync(DATABASE_ID);
        }
    }
}
