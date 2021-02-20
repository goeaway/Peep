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

        private const int DATABASE_ID = 1;

        public CrawlFilterManager(
            IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async Task Clear()
        {
            var server = _connection.GetServer("localhost:6379");

            await server.FlushDatabaseAsync(DATABASE_ID);
        }

        public Task<int> GetCount()
        {
            return Task.FromResult(_connection
                .GetServer("localhost:6379")
                .Keys(DATABASE_ID)
                .Count());
        }
    }
}
