using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Persistence;
using Peep.Core;
using Serilog;

namespace Peep.API.Application
{
    public class HostedCrawlerService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICrawler _crawler;
        //private readonly IDistributedCache _cache;
        private readonly PeepApiContext _context;

        public HostedCrawlerService(
            PeepApiContext context,
            ILogger logger,
            //IDistributedCache cache,
            ICrawler crawler
            )
        {
            _context = context;
            _logger = logger;
            _crawler = crawler;
            //_cache = cache;
        }

        private bool TryGetJob(out CrawlJob job, out string jobId)
        {
            if (_context.QueuedJobs.Any())
            {
                var nextJob = _context.QueuedJobs.OrderBy(qj => qj.DateQueued).First();
                job = JsonConvert.DeserializeObject<CrawlJob>(nextJob.JobJson);
                jobId = nextJob.Id;

                _context.QueuedJobs.Remove(nextJob);
                _context.SaveChanges();
                return true;
            }

            job = null;
            jobId = null;
            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");
            while (!stoppingToken.IsCancellationRequested)
            {
                // try get job from repository
                var jobFound = TryGetJob(out var job, out var jobId);

                if (jobFound)
                {
                    try
                    {
                        // use crawler
                        var progressTimeSpan = TimeSpan.FromMinutes(1);
                        // enrich logs in here with job id
                        _logger.Information("Running job {Id}", jobId);
                        //var result = await _crawler.Crawl(
                        //    job,
                        //    progressTimeSpan,
                        //    async progress =>
                        //    {
                        //        // update the cache with progress for this job
                        //        //await _cache.SetStringAsync(
                        //        //    "progress:" + job.Id,
                        //        //    JsonConvert.SerializeObject(progress),
                        //        //    new DistributedCacheEntryOptions
                        //        //    {
                        //        //        AbsoluteExpirationRelativeToNow = progressTimeSpan
                        //        //    });
                        //    },
                        //    stoppingToken);

                        _logger.Information("Saving result");
                        // cache the job result for X ttl
                        //await _cache.SetStringAsync(
                        //    "results:" + job.Id,
                        //    JsonConvert.SerializeObject(result),
                        //    new DistributedCacheEntryOptions
                        //    {
                        //        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                        //    });
                    } 
                    catch (Exception e)
                    {

                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
