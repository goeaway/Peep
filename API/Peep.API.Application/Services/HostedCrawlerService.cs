using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Application.Providers;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Exceptions;
using Serilog;

namespace Peep.API.Application
{
    public class HostedCrawlerService : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly ICrawler _crawler;
        private readonly INowProvider _nowProvider;
        private readonly PeepApiContext _context;

        public HostedCrawlerService(
            PeepApiContext context,
            ILogger logger,
            ICrawler crawler,
            INowProvider nowProvider
            )
        {
            _context = context;
            _logger = logger;
            _crawler = crawler;
            _nowProvider = nowProvider;
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
            _logger.Information("Waiting for jobs");
            while (!stoppingToken.IsCancellationRequested)
            {
                // try get job from repository
                var jobFound = TryGetJob(out var queuedJob);

                if (jobFound)
                {
                    // enrich logs in here with job id
                    _logger.Information("Running job {Id}", queuedJob.Id);
                    var crawlJob = JsonConvert.DeserializeObject<CrawlJob>(queuedJob.JobJson);

                    var startTime = _nowProvider.Now;
                    
                    try
                    {
                        var result = await _crawler.Crawl(
                            crawlJob,
                            stoppingToken);

                        _logger.Information("Saving result");

                        _context.CompletedJobs.Add(new CompletedJob
                        {
                            JobJson = queuedJob.JobJson,
                            Id = queuedJob.Id,
                            CrawlCount = result.CrawlCount,
                            DataJson = JsonConvert.SerializeObject(result.Data),
                            Duration = result.Duration,
                            DateQueued = queuedJob.DateQueued,
                            DateStarted = startTime,
                            DateCompleted = _nowProvider.Now,
                        });
                    }
                    catch (CrawlerRunException e)
                    {
                        _context.ErroredJobs.Add(new ErroredJob
                        {
                            Id = queuedJob.Id,
                            ErrorMessage = e.Message,
                            JobJson = queuedJob.JobJson,
                            DateStarted = startTime,
                            DateCompleted = _nowProvider.Now,
                            DateQueued = queuedJob.DateQueued,
                            DataJson = JsonConvert.SerializeObject(e.CrawlResult.Data),
                            CrawlCount = e.CrawlResult.CrawlCount,
                            Duration = e.CrawlResult.Duration
                        });
                    }
                    catch (Exception e)
                    {
                        _context.ErroredJobs.Add(new ErroredJob
                        {
                            Id = queuedJob.Id,
                            ErrorMessage = e.Message,
                            JobJson = queuedJob.JobJson,
                            DateStarted = startTime,
                            DateCompleted = _nowProvider.Now,
                            DateQueued = queuedJob.DateQueued,
                        });
                    }
                    finally
                    {
                        _context.SaveChanges();
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
