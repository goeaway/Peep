using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Application.Options;
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
        private readonly CrawlConfigOptions _options;
        private readonly ICrawlCancellationTokenProvider _tokenProvider;

        public HostedCrawlerService(
            PeepApiContext context,
            ILogger logger,
            ICrawler crawler,
            INowProvider nowProvider,
            CrawlConfigOptions options,
            ICrawlCancellationTokenProvider tokenProvider)
        {
            _context = context;
            _logger = logger;
            _crawler = crawler;
            _nowProvider = nowProvider;
            _options = options;
            _tokenProvider = tokenProvider;
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
                    var runningJob = new RunningJob
                    {
                        Id = queuedJob.Id,
                        JobJson = queuedJob.JobJson,
                        DateQueued = queuedJob.DateQueued,
                        DateStarted = _nowProvider.Now,
                    };

                    _context.RunningJobs.Add(runningJob);
                    _context.SaveChanges();

                    // enrich logs in here with job id
                    _logger.Information("Running job {Id}", queuedJob.Id);
                    var crawlJob = JsonConvert.DeserializeObject<CrawlJob>(queuedJob.JobJson);

                    var startTime = _nowProvider.Now;

                    // create a linked token source so we can stop the crawl if the service needs to stop
                    // or the job needs to stop
                    var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken,
                        _tokenProvider.GetToken(queuedJob.Id));
                    
                    try
                    {
                        var channelReader = _crawler.Crawl(
                            crawlJob,
                            TimeSpan.FromMilliseconds(_options.ProgressUpdateMilliseconds),
                            cancellationTokenSource.Token);

                        // async iterate over channel's results
                        // update the running jobs running totals of the crawl result
                        await foreach(var result in channelReader.ReadAllAsync(cancellationTokenSource.Token))
                        {
                            runningJob.DataJson = JsonConvert.SerializeObject(result.Data);
                            runningJob.CrawlCount = result.CrawlCount;
                            runningJob.Duration = result.Duration;

                            _context.SaveChanges();
                        }

                        _logger.Information("Saving result");

                        // build a completed job record based off the running job values
                        _context.CompletedJobs.Add(new CompletedJob
                        {
                            JobJson = queuedJob.JobJson,
                            Id = queuedJob.Id,
                            CrawlCount = runningJob.CrawlCount,
                            DataJson = runningJob.DataJson,
                            Duration = runningJob.Duration,
                            DateQueued = queuedJob.DateQueued,
                            DateStarted = startTime,
                            DateCompleted = _nowProvider.Now,
                        });

                        _context.RunningJobs.Remove(runningJob);
                        _tokenProvider.DisposeOfToken(queuedJob.Id);
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
