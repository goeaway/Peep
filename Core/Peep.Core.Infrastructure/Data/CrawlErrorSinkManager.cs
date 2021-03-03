using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Peep.Core.API.Options;
using Peep.Data;
using StackExchange.Redis;

namespace Peep.Core.Infrastructure.Data
{
    public class CrawlErrorSinkManager : ICrawlDataSinkManager<CrawlErrors>
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly CachingOptions _cachingOptions;

        private const int DATABASE_ID = 4;

        public CrawlErrorSinkManager(
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

        public async Task<CrawlErrors> GetData(string jobId)
        {
            var server = _connection.GetServer($"{_cachingOptions.Hostname}:{_cachingOptions.Port}");

            var jobKeys = server.Keys(DATABASE_ID, $"{jobId}.*").ToArray();

            var values = await _connection.GetDatabase(DATABASE_ID).StringGetAsync(jobKeys);

            var result = new CrawlErrors();
            result.AddRange(values
                .SelectMany(value => JsonConvert.DeserializeObject<CrawlErrors>(value.ToString())));

            return result;
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