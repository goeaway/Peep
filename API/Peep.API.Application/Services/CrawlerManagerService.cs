using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using MassTransit;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Serilog;
using Serilog.Context;

namespace Peep.API.Application.Services
{
    public class CrawlerManagerService : BackgroundService
    {
        private PeepApiContext _context;
        private ILogger _logger;
        private INowProvider _nowProvider;
        private ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;
        private ICrawlDataSinkManager _dataManager;
        private ICrawlFilterManager _filterManager;
        private ICrawlQueueManager _queueManager;
        private IPublishEndpoint _publishEndpoint;
        
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
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider,
            IPublishEndpoint publishEndpoint,
            ICrawlFilterManager filterManager,
            ICrawlQueueManager queueManager,
            ICrawlDataSinkManager dataManager)
        {
            _logger = logger;
            _context = context;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _publishEndpoint = publishEndpoint;
            _dataManager = dataManager;
            _queueManager = queueManager;
            _filterManager = filterManager;
        }

        private IServiceScope SetServices(IServiceScope serviceScope)
        {
            if(serviceScope != null)
            {
                _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger>();
                _nowProvider = serviceScope.ServiceProvider.GetRequiredService<INowProvider>();
                _context = serviceScope.ServiceProvider.GetRequiredService<PeepApiContext>();
                _crawlCancellationTokenProvider = serviceScope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
                _publishEndpoint = serviceScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                _dataManager = serviceScope.ServiceProvider.GetRequiredService<ICrawlDataSinkManager>();
                _filterManager = serviceScope.ServiceProvider.GetRequiredService<ICrawlFilterManager>();
                _queueManager = serviceScope.ServiceProvider.GetRequiredService<ICrawlQueueManager>();
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
                    using (LogContext.PushProperty("JobId", job.Id))
                    {
                        try
                        {
                            _logger.Information("Running job");

                            // save running job
                            _context.RunningJobs.Add(new RunningJob
                            {
                                Id = job.Id,
                                JobJson = job.JobJson,
                                DateQueued = job.DateQueued,
                                DateStarted = _nowProvider.Now
                            });

                            _context.SaveChanges();

                            var stoppableCrawlJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(job.JobJson);
                            // queue up job seeds
                            _logger.Information("Queueing seeds {Seeds}", string.Join(", ", stoppableCrawlJob.Seeds));
                            await _queueManager.Enqueue(stoppableCrawlJob.Seeds);

                            var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                                stoppingToken, _crawlCancellationTokenProvider.GetToken(job.Id));

                            var crawlJob = stoppableCrawlJob as CrawlJob;
                            var identifiableCrawlJob = new IdentifiableCrawlJob(crawlJob, job.Id);

                            var dateStarted = _nowProvider.Now;

                            _logger.Information("Publishing job to crawlers");
                            await _publishEndpoint
                                .Publish(
                                    new CrawlQueued
                                    {  
                                        Job = identifiableCrawlJob
                                    },
                                    combinedCancellationTokenSource.Token);
                            var stopConditionMet = false;

                            // go into loop checking for job result + cancellation
                            while (!combinedCancellationTokenSource.IsCancellationRequested)
                            {
                                // assess duration, crawl count from filter
                                // and total data (crawlers should provide easily accessible count of data in each 
                                // push, so we can aggregate easier without having to serialise the data here each time)
                                // check that info against stop conditions
                                var result = new CrawlResult
                                {
                                    CrawlCount = await _filterManager.GetCount(),
                                    DataCount = await _dataManager.GetCount(job.Id),
                                    Duration = _nowProvider.Now - dateStarted
                                };

                                if (stoppableCrawlJob.StopConditions.Any(sc => sc.Stop(result)))
                                {
                                    combinedCancellationTokenSource.Cancel();
                                    stopConditionMet = true;
                                    break;
                                }

                                await Task.Delay(500, combinedCancellationTokenSource.Token);
                            }

                            // the only way to be here is if the token source is cancelled
                            // so broadcast cancellation to crawlers
                            // give crawlers a chance to finish up and respond somehow (maybe an EOF push in the data cache?)
                            _logger.Information("Cancelling job");
                            await _publishEndpoint
                                .Publish(
                                    new CrawlCancelled
                                    {
                                        CrawlId = job.Id
                                    },
                                    cancellationToken: stoppingToken);

                            // crawlers will have placed their found data in cache as events
                            // we should gather them all up for the finished data set
                            var data = await _dataManager.GetData(job.Id);

                            _logger.Information("Completing job");
                            // create and save completed job with data
                            _context.CompletedJobs.Add(new CompletedJob
                            {
                                Id = job.Id,
                                DateQueued = job.DateQueued,
                                DateStarted = dateStarted,
                                DateCompleted = _nowProvider.Now,
                                Duration = _nowProvider.Now - dateStarted,
                                JobJson = job.JobJson,
                                CompletionReason =
                                    stopConditionMet ? CrawlCompletionReason.StopConditionMet :
                                    false ? CrawlCompletionReason.Error :
                                    CrawlCompletionReason.Cancelled,
                                CrawlCount = await _filterManager.GetCount(),
                                DataJson = JsonConvert.SerializeObject(data),
                            });

                            var running = _context.RunningJobs.Find(job.Id);
                            _context.RunningJobs.Remove(running);

                            _context.SaveChanges();

                            _logger.Information("Cleaning up after job");
                            // clear cache of data, queue, filter
                            await _dataManager.Clear(job.Id);
                            await _queueManager.Clear();
                            await _filterManager.Clear();

                            _crawlCancellationTokenProvider.DisposeOfToken(job.Id);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Error occurred when trying to process job");
                        }
                    }
                } 
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
