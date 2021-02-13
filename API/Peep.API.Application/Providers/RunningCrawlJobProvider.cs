using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Peep.API.Models.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.API.Application.Providers
{
    public class RunningCrawlJobProvider : IRunningCrawlJobProvider
    {
        private readonly IDistributedCache _cache;
        public RunningCrawlJobProvider(IDistributedCache cache)
        {
            _cache = cache;
        }

        private string GetJobKey (string id)
        {
            return $"running:{id}";
        }

        public async Task<RunningJob> GetRunningJob(string id)
        {
            if(id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var jobString = await _cache.GetStringAsync(GetJobKey(id));
            if(jobString != null)
            {
                return JsonConvert.DeserializeObject<RunningJob>(jobString);
            }

            return null;
        }

        public async Task SaveJob(RunningJob job)
        {
            if(job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            await _cache.SetStringAsync(GetJobKey(job.Id), JsonConvert.SerializeObject(job));
        }

        public async Task RemoveJob(string id)
        {
            if(id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            await _cache.RemoveAsync(GetJobKey(id));
        }
    }
}
