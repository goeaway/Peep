using Peep.Core.API.Options;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using Peep.Core.Filtering;

namespace Peep.Core.Infrastructure.Filtering
{
    public class CacheCrawlFilter : ICrawlFilter
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly CachingOptions _cachingOptions;

        private const int DATABASE_ID = 1;

        public CacheCrawlFilter(
            IConnectionMultiplexer connection,
            CachingOptions cachingOptions)
        {
            _connection = connection;
            _cachingOptions = cachingOptions;
        }

        public int Count =>
            _connection
                .GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}")
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

            return !(await db.StringGetAsync(uri)).IsNull;
        }
    }
}
