using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Peep.Core.Infrastructure.Data
{
    public class CacheCrawlErrorSink : ICrawlDataSink<CrawlError>
    {
        private readonly IConnectionMultiplexer _connection;

        private const int DATABASE_ID = 4;

        public CacheCrawlErrorSink(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async Task Push(string jobId, CrawlError data)
        {
            var db = _connection.GetDatabase(DATABASE_ID);

            await db.StringSetAsync(
                $"{jobId}.{Guid.NewGuid()}", 
                JsonConvert.SerializeObject(data));
        }
    }
}