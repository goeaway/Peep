using Newtonsoft.Json;
using Peep.Core.API.Options;
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
        private readonly CachingOptions _cachingOptions;

        private const int DATABASE_ID = 3;

        public CrawlDataSinkManager(
            IConnectionMultiplexer connection,
            CachingOptions cachingOptions)
        {
            _connection = connection;
            _cachingOptions = cachingOptions;
        }

        public Task<int> GetCount(string jobId)
        {
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            var jobKeys = server.Keys(DATABASE_ID, $"{jobId}.*");

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
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            var jobKeys = server.Keys(DATABASE_ID, $"{jobId}.*").ToArray();

            var values = await _connection.GetDatabase(DATABASE_ID).StringGetAsync(jobKeys);

            return values
                .Select(value => JsonConvert.DeserializeObject<IDictionary<Uri, IEnumerable<string>>>(value))
                .SelectMany(data => data)
                .ToDictionary(item => item.Key, item => item.Value);
        }

        public async Task Clear(string jobId)
        {
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            foreach (var key in server.Keys(DATABASE_ID, $@"{jobId}.*"))
            {
                await _connection.GetDatabase(DATABASE_ID).KeyDeleteAsync(key);
            }
        }
    }
}
