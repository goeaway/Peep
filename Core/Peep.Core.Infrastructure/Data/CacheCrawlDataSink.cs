using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public class CacheCrawlDataSink : ICrawlDataSink
    {
        private readonly IConnectionMultiplexer _connection;

        private const int DATABASE_ID = 3;

        public CacheCrawlDataSink(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public async Task Push(string jobId, IDictionary<Uri, IEnumerable<string>> data)
        {
            var db = _connection.GetDatabase(DATABASE_ID);

            await db.StringSetAsync(
                $"{jobId}.{Guid.NewGuid()}.{data.Count}", 
                JsonConvert.SerializeObject(data));
        }
    }
}
