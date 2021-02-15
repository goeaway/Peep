using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Crawler.Infrastructure
{
    public class DistributedCacheRepository<T> : IGenericRepository<T>
    {
        private readonly IDistributedCache _cache;
        private readonly Func<string, string> _keyGenerator;

        public DistributedCacheRepository(
            IDistributedCache cache,
            Func<string, string> keyGenerator)
        {
            _cache = cache;
            _keyGenerator = keyGenerator;
        }

        public async Task<T> Get(string id)
        {
            if(id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return JsonConvert.DeserializeObject<T>(await _cache.GetStringAsync(_keyGenerator(id)));
        }

        public async Task Set(string id, T data)
        {
            if(id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            await _cache.SetStringAsync(_keyGenerator(id), JsonConvert.SerializeObject(data));
        }
    }
}
