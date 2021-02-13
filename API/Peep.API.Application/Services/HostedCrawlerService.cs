using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.API.Application.Options;
using Peep.API.Application.Providers;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Exceptions;
using Serilog;

namespace Peep.API.Application.Services
{
    public class HostedCrawlerService : BackgroundService
    {
        private ILogger _logger;
        private ICrawler _crawler;
        private INowProvider _nowProvider;
        private PeepApiContext _context;
        private CrawlConfigOptions _options;
        private ICrawlCancellationTokenProvider _tokenProvider;
        private IRunningCrawlJobProvider _runningCrawlJobRepository;

        private readonly IServiceProvider _serviceProvider;

        public HostedCrawlerService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public HostedCrawlerService(
            PeepApiContext context,
            ILogger logger,
            ICrawler crawler,
            INowProvider nowProvider,
            CrawlConfigOptions options,
            ICrawlCancellationTokenProvider tokenProvider,
            IRunningCrawlJobProvider runningCrawlJobRepository)
        {
            _context = context;
            _logger = logger;
            _crawler = crawler;
            _nowProvider = nowProvider;
            _options = options;
            _tokenProvider = tokenProvider;
            _runningCrawlJobRepository = runningCrawlJobRepository;
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

        private void SetServices(IServiceScope serviceScope)
        {
            if(serviceScope == null)
            {
                return;
            }

            _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger>();
            _crawler = serviceScope.ServiceProvider.GetRequiredService<ICrawler>();
            _nowProvider = serviceScope.ServiceProvider.GetRequiredService<INowProvider>();
            _context = serviceScope.ServiceProvider.GetRequiredService<PeepApiContext>();
            _options = serviceScope.ServiceProvider.GetRequiredService<CrawlConfigOptions>();
            _tokenProvider = serviceScope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
            _runningCrawlJobRepository = serviceScope.ServiceProvider.GetRequiredService<IRunningCrawlJobProvider>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider?.CreateScope();
            SetServices(scope);
            _logger.Information("Waiting for jobs");

            while (!stoppingToken.IsCancellationRequested)
            {
                // try get job from repository
                var jobFound = TryGetJob(out var queuedJob);

                if (jobFound)
                {
                    await _runningCrawlJobRepository.SaveJob(new RunningJob
                    {
                        Id = queuedJob.Id,
                        JobJson = queuedJob.JobJson,
                        DateQueued = queuedJob.DateQueued,
                        DateStarted = _nowProvider.Now,
                    });

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
                            TimeSpan.FromMilliseconds(Math.Min(_options.ProgressUpdateMilliseconds, 100)),
                            cancellationTokenSource.Token);

                        // async iterate over channel's results
                        // update the running jobs running totals of the crawl result
                        await foreach(var result in channelReader.ReadAllAsync(cancellationTokenSource.Token))
                        {
                            // find running in cache
                            var rJ = await _runningCrawlJobRepository.GetRunningJob(queuedJob.Id);

                            // update values
                            rJ.DataJson = JsonConvert.SerializeObject(result.Data);
                            rJ.CrawlCount = result.CrawlCount;
                            rJ.Duration = result.Duration;

                            // save 
                            await _runningCrawlJobRepository.SaveJob(rJ);
                        }

                        _logger.Information("Saving result");

                        var runningJob = await _runningCrawlJobRepository.GetRunningJob(queuedJob.Id);

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
                    }
                    catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                    {
                        var runningJob = await _runningCrawlJobRepository.GetRunningJob(queuedJob.Id);

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
                        await _runningCrawlJobRepository.RemoveJob(queuedJob.Id);
                        _tokenProvider.DisposeOfToken(queuedJob.Id);

                        _context.SaveChanges();
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
