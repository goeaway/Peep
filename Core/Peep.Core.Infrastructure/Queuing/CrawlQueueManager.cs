using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Queuing
{
    public class CrawlQueueManager : ICrawlQueueManager
    {
        private readonly IConnectionMultiplexer _connection;

        private const int DATABASE_ID = 2;

        public CrawlQueueManager(
            IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async Task Clear()
        {
            var server = _connection.GetServer("localhost:6379");

            await server.FlushDatabaseAsync(DATABASE_ID);
        }
    }
}
