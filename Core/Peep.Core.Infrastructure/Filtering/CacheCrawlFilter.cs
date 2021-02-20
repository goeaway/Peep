using Peep.Filtering;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Filtering
{
    public class CacheCrawlFilter : ICrawlFilter
    {
        private readonly IConnectionMultiplexer _connection;

        private const int DATABASE_ID = 1;

        public CacheCrawlFilter(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public int Count =>
            _connection
                .GetServer("localhost:6379")
                .Keys(DATABASE_ID)
                .Count();

        public async Task Add(string uri)
        {
            var db = _connection.GetDatabase(DATABASE_ID);

            await db.StringSetAsync(uri, "", flags: CommandFlags.FireAndForget);
        }

        public async Task<bool> Contains(string uri)
        {
            var db = _connection.GetDatabase(DATABASE_ID);

            return (await db.StringGetAsync(uri)).IsNull;
        }
    }
}
