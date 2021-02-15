using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Application.Providers;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Core.API.Providers;
using Peep.Exceptions;
using Serilog;

namespace Peep.API.Application.Services
{
    public class CrawlerManagerService : BackgroundService
    {
        private PeepApiContext _context;
        private ILogger _logger;
        private INowProvider _nowProvider;
        private ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;

        private readonly IServiceProvider _serviceProvider;

        public CrawlerManagerService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public CrawlerManagerService(
            PeepApiContext context,
            ILogger logger,
            INowProvider nowProvider,
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider)
        {
            _logger = logger;
            _context = context;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
        }

        private IServiceScope SetServices(IServiceScope serviceScope)
        {
            if(serviceScope != null)
            {
                _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger>();
                _nowProvider = serviceScope.ServiceProvider.GetRequiredService<INowProvider>();
                _context = serviceScope.ServiceProvider.GetRequiredService<PeepApiContext>();
                _crawlCancellationTokenProvider = serviceScope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
            }
            
            return serviceScope;
        }

        private bool TryGetJob(out QueuedJob job)
        {
            if (_context.QueuedJobs.Any()) 
            {
                job = _context.QueuedJobs.OrderBy(qj => qj.DateQueued).First();

                _context.QueuedJobs.Remove(job);
                _context.SaveChanges();

                return true;
            }

            job = null;
            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = SetServices(_serviceProvider?.CreateScope());
            _logger.Information("Waiting for jobs");

            while (!stoppingToken.IsCancellationRequested)
            {
                // check for job
                var foundJob = TryGetJob(out var job);

                if(foundJob)
                {
                    var duration = new Stopwatch();
                    duration.Start();

                    var crawlJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(job.JobJson);
                    // add job seeds to queue
                    // broadcast job start to crawlers

                    // go into loop checking for job result + cancellation
                    var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken, _crawlCancellationTokenProvider.GetToken(job.Id));

                    while(!combinedCancellationTokenSource.IsCancellationRequested)
                    {
                        // assess duration, crawl count from filter
                        // and total data (crawlers should provide easily accessible count of data in each 
                        // push, so we can aggregate easier without having to serialise the data here each time)
                        // check that info against stop conditions
                        // if crawl should stop by stop condition, set the token source here
                        var result = new CrawlResult
                        {
                            CrawlCount = 1,
                            Duration = duration.Elapsed,
                        };

                        foreach(var stopCondition in crawlJob.StopConditions)
                        {
                            if(stopCondition.Stop(result))
                            {
                                combinedCancellationTokenSource.Cancel();
                                break;
                            }
                        }

                        await Task.Delay(1000);
                    }

                    // the only way to be here is if the token source is cancelled
                    // so broadcast cancellation to crawlers
                    // give crawlers a chance to finish up and respond somehow (maybe an EOF push in the data cache?)

                    // crawlers will have placed their found data in cache as events
                    // we should gather them all up for the finished data set

                    // create and save completed job with data
                    // clear cache of data, queue, filter
                    // + any error reporting if required
                } 
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
