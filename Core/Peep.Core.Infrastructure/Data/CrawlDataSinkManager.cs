using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public class CrawlDataSinkManager : ICrawlDataSinkManager
    {
        private readonly IConnectionMultiplexer _connection;

        private const int DATABASE_ID = 3;

        public CrawlDataSinkManager(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }

        public Task<int> GetCount(string jobId)
        {
            var server = _connection.GetServer("localhost:6379");

            var jobKeys = server.Keys(DATABASE_ID, $"{jobId}.");

            return Task.FromResult(
                jobKeys
                .Sum(jk => Convert
                    .ToInt32(jk
                        .ToString()
                        .Split(".")
                        .Last())));
        }

        public async Task<IDictionary<Uri, IEnumerable<string>>> GetData(string jobId)
        {
            var server = _connection.GetServer("localhost:6379");

            var jobKeys = server.Keys(DATABASE_ID, $@"{jobId}\..*?").ToArray();

            var values = await _connection.GetDatabase(DATABASE_ID).StringGetAsync(jobKeys);

            var result = new Dictionary<Uri, IEnumerable<string>>();

            foreach (var value in values)
            {
                // deserialise and concatenate
                result.Concat(JsonConvert.DeserializeObject<IDictionary<Uri, IEnumerable<string>>>(value));
            }

            return result;
        }

        public async Task Clear(string jobId)
        {
            var server = _connection.GetServer("localhost:6379");

            foreach(var key in server.Keys(DATABASE_ID, $@"{jobId}\.*?"))
            {
                await _connection.GetDatabase(DATABASE_ID).KeyDeleteAsync(key);
            }
        }
    }
}
