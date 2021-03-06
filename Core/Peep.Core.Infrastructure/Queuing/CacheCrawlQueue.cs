using Peep.Queueing;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using RedLockNet;
using RedLockNet.SERedis;

namespace Peep.Core.Infrastructure.Queuing
{
    public class CacheCrawlQueue : ICrawlQueue
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly IDistributedLockFactory _lockFactory;

        private const string QUEUE_KEY = "crawlqueue";
        private const int DATABASE_ID = 2;

        public CacheCrawlQueue(
            IConnectionMultiplexer connection, 
            IDistributedLockFactory lockFactory)
        {
            _connection = connection;
            _lockFactory = lockFactory;
        }

        public async Task<Uri> Dequeue()
        {
            using var redLock = await _lockFactory
                .CreateLockAsync(
                    QUEUE_KEY, 
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(500));

            if (!redLock.IsAcquired)
            {
                return null;
            } 
                
            var cacheResult = await _connection
                .GetDatabase(DATABASE_ID)
                .ListLeftPopAsync(QUEUE_KEY);

            return cacheResult.IsNullOrEmpty ? null : new Uri(cacheResult);
        }

        public async Task Enqueue(Uri uri)
        {
            using var redLock = await _lockFactory
                .CreateLockAsync(
                    QUEUE_KEY, 
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromMilliseconds(200),
                    TimeSpan.FromMilliseconds(500));

            if (redLock.IsAcquired)
            {
                await _connection
                    .GetDatabase(DATABASE_ID)
                    .ListRightPushAsync(
                        QUEUE_KEY, 
                        uri.AbsoluteUri);
            } 
        }
    }
}
