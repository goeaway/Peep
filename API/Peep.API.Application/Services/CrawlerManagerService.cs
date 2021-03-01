using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using MassTransit;
using Peep.API.Application.Providers;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Persistence;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.Data;
using Serilog;
using Serilog.Context;

namespace Peep.API.Application.Services
{
    public class CrawlerManagerService : BackgroundService
    {
        private readonly PeepApiContext _context;
        private readonly ILogger _logger;
        private readonly INowProvider _nowProvider;
        private readonly ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;
        private readonly ICrawlDataSinkManager<ExtractedData> _dataManager;
        private readonly ICrawlDataSinkManager<CrawlErrors> _errorManager;
        private readonly ICrawlFilterManager _filterManager;
        private readonly ICrawlQueueManager _queueManager;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IQueuedJobProvider _jobProvider;
        
        public CrawlerManagerService(
            PeepApiContext context,
            ILogger logger,
            INowProvider nowProvider,
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider,
            IPublishEndpoint publishEndpoint,
            ICrawlFilterManager filterManager,
            ICrawlQueueManager queueManager,
            ICrawlDataSinkManager<ExtractedData> dataManager,
            ICrawlDataSinkManager<CrawlErrors> errorManager,
            IQueuedJobProvider jobProvider)
        {
            _logger = logger;
            _context = context;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _publishEndpoint = publishEndpoint;
            _dataManager = dataManager;
            _queueManager = queueManager;
            _filterManager = filterManager;
            _errorManager = errorManager;
            _jobProvider = jobProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Waiting for jobs");

            while (!stoppingToken.IsCancellationRequested)
            {
                // check for job
                var jobFound = _jobProvider.TryGetJob(out var job, out var jobData);

                if(jobFound)
                {
                    using (LogContext.PushProperty("JobId", job.Id))
                    {
                        _logger.Information("Running job");
                        await RunJob(
                            job,
                            jobData, 
                            stoppingToken);
                        _logger.Information("Finished Job");
                    }
                } 
                else
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }

        private async Task RunJob(
            QueuedJob job,
            StoppableCrawlJob jobData, 
            CancellationToken stoppingToken)
        {
            var stopConditionMet = false;
            var dateStarted = _nowProvider.Now;

            try
            {
                // clear these first in case previous runs did not
                await _dataManager.Clear(job.Id);
                await _queueManager.Clear();
                await _filterManager.Clear();
                
                await _context.RunningJobs.AddAsync(new RunningJob
                {
                    Id = job.Id,
                    JobJson = job.JobJson,
                    DateQueued = job.DateQueued,
                    DateStarted = dateStarted
                });

                await _context.SaveChangesAsync();

                // queue up job seeds
                _logger.Information("Queueing seeds {Seeds}", string.Join(", ", jobData.Seeds));
                await _queueManager.Enqueue(jobData.Seeds);

                var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    stoppingToken, _crawlCancellationTokenProvider.GetToken(job.Id));

                var identifiableCrawlJob = new IdentifiableCrawlJob(jobData, job.Id);

                _logger.Information("Publishing job to crawlers");
                await _publishEndpoint
                    .Publish(
                        new CrawlQueued
                        {
                            Job = identifiableCrawlJob
                        },
                        combinedCancellationTokenSource.Token);

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

                    if (jobData.StopConditions.Any(sc => sc.Stop(result)))
                    {
                        combinedCancellationTokenSource.Cancel();
                        stopConditionMet = true;
                        break;
                    }

                    var running = await _context.RunningJobs.FindAsync(job.Id);
                    running.Duration = result.Duration;
                    running.CrawlCount = result.CrawlCount;

                    await _context.SaveChangesAsync(combinedCancellationTokenSource.Token);

                    await Task.Delay(500, combinedCancellationTokenSource.Token);
                }
            }
            catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
            {
                // do nothing for these                
            }
            catch (Exception e)  
            {
                _logger.Error(e, "Error occurred when trying to process job");
            }
            finally
            {
                // send cancellation message to stop crawlers
                await _publishEndpoint
                    .Publish(
                        new CrawlCancelled
                        {
                            CrawlId = job.Id
                        }, CancellationToken.None);
                
                // crawlers will have placed their found data in cache as events
                // we should gather them all up for the finished data set
                var data = await _dataManager.GetData(job.Id);
                var errors = await _errorManager.GetData(job.Id) ?? new CrawlErrors();

                _logger.Information("Saving data");
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
                        errors.Any() ? CrawlCompletionReason.Error :
                        CrawlCompletionReason.Cancelled,
                    CrawlCount = await _filterManager.GetCount(),
                    DataJson = JsonConvert.SerializeObject(data),
                    ErrorMessage = string.Join(",", errors.Select(e => e.Exception.Message))
                });

                // removing running job item
                _logger.Information("Cleaning up");
                var running = _context.RunningJobs.Find(job.Id);
                _context.RunningJobs.Remove(running);

                await _context.SaveChangesAsync();

                // clear cache of data, queue, filter
                await _dataManager.Clear(job.Id);
                await _queueManager.Clear();
                await _filterManager.Clear();

                _crawlCancellationTokenProvider.DisposeOfToken(job.Id);
            }
        }
    }
}
