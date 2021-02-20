using Peep.Queueing;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Queuing
{
    public class CacheCrawlQueue : ICrawlQueue
    {
        private readonly IConnectionMultiplexer _connection;

        private const string QUEUE_KEY = "crawlqueue";
        private const int DATABASE_ID = 2;

        public CacheCrawlQueue(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async Task<Uri> Dequeue()
        {
            var cacheResult = await _connection.GetDatabase(DATABASE_ID).ListLeftPopAsync(QUEUE_KEY);

            if (cacheResult.IsNullOrEmpty)
            {
                return null;
            }

            return new Uri(cacheResult);
        }

        public async Task Enqueue(Uri uri)
        {
            await _connection.GetDatabase(DATABASE_ID).ListRightPushAsync(QUEUE_KEY, uri.AbsoluteUri);
        }
    }
}
