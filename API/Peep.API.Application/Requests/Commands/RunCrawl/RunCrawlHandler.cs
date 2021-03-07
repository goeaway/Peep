using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Peep.API.Application.Managers;
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

namespace Peep.API.Application.Requests.Commands.RunCrawl
{
    public class RunCrawlHandler : IRequestHandler<RunCrawlRequest, Unit>
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
        private readonly ICrawlerManager _crawlerManager;
        
        public RunCrawlHandler(
            PeepApiContext context, 
            ILogger logger, 
            INowProvider nowProvider, 
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider, 
            ICrawlDataSinkManager<ExtractedData> dataManager, 
            ICrawlDataSinkManager<CrawlErrors> errorManager, 
            ICrawlFilterManager filterManager, 
            ICrawlQueueManager queueManager, 
            IPublishEndpoint publishEndpoint, 
            ICrawlerManager crawlerManager)
        {
            _context = context;
            _logger = logger;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _dataManager = dataManager;
            _errorManager = errorManager;
            _filterManager = filterManager;
            _queueManager = queueManager;
            _publishEndpoint = publishEndpoint;
            _crawlerManager = crawlerManager;
        }
        
        public async Task<Unit> Handle(RunCrawlRequest request, CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("JobId", request.Job.Id))
            {
                _logger.Information("Running job");
                var stopConditionMet = false;
                var dateStarted = _nowProvider.Now;

                try
                {
                    var cancellationTokenSource = await StartJob(request.Job, request.JobData, cancellationToken, dateStarted);

                    stopConditionMet = await CheckProgress(request.Job, request.JobData, cancellationTokenSource, dateStarted);
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
                    await CleanupAfterJob(request.Job, cancellationToken, dateStarted, stopConditionMet);
                }
                _logger.Information("Finished Job");
            }
            
            return Unit.Value;
        }
        
        private async Task<CancellationTokenSource> StartJob(
            QueuedJob job, 
            CrawlJob jobData, 
            CancellationToken stoppingToken,
            DateTime dateStarted)
        {
            await _dataManager.Clear(job.Id);
            await _queueManager.Clear();
            await _filterManager.Clear();

            await _context.RunningJobs.AddAsync(new RunningJob
            {
                Id = job.Id,
                JobJson = job.JobJson,
                DateQueued = job.DateQueued,
                DateStarted = dateStarted
            }, stoppingToken);

            await _context.SaveChangesAsync(stoppingToken);

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
            return combinedCancellationTokenSource;
        }
        
        private async Task<bool> CheckProgress(
            QueuedJob job, 
            StoppableCrawlJob jobData,
            CancellationTokenSource combinedCancellationTokenSource, 
            DateTime dateStarted)
        {
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
                    _logger.Information("Stop condition reached, stopping crawl");
                    combinedCancellationTokenSource.Cancel();
                    return true;
                }

                var running = await _context.RunningJobs.FindAsync(job.Id);
                running.Duration = result.Duration;
                running.CrawlCount = result.CrawlCount;

                await _context.SaveChangesAsync(combinedCancellationTokenSource.Token);

                await Task.Delay(500, combinedCancellationTokenSource.Token);
            }

            return false;
        }

        private async Task CleanupAfterJob(QueuedJob job, CancellationToken stoppingToken, DateTime dateStarted,
            bool stopConditionMet)
        {
            // send cancellation message to stop crawlers
            await _publishEndpoint
                .Publish(
                    new CrawlCancelled
                    {
                        CrawlId = job.Id
                    }, CancellationToken.None);

            var errors = new CrawlErrors();
            
            try
            {
                // wait in a loop here until all crawlers involved in job say they're done
                // or timeout happens
                _logger.Information("Waiting for crawlers to complete");
                await _crawlerManager.WaitAllFinished(job.Id, TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException e)
            {
                errors.Add(new CrawlError { Message =  e.Message });
            }

            var errorData = await _errorManager.GetData(job.Id);
            if (errorData != null)
            {
                errors.AddRange(errorData);
            }

            _logger.Information("Saving data");
            // create and save completed job with data
            var completedJob = new CompletedJob
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
                ErrorMessage = string.Join(", ", errors.Select(e => e.Message))
            };

            // crawlers will have placed their found data in cache as events
            // we should gather them all up for the finished data set
            completedJob.CompletedJobData = await GetData(job.Id, completedJob);

            await _context.CompletedJobs.AddAsync(completedJob, stoppingToken);

            // removing running job item
            _logger.Information("Cleaning up");
            var running = await _context.RunningJobs.FindAsync(job.Id);
            _context.RunningJobs.Remove(running);

            await _context.SaveChangesAsync(stoppingToken);

            // clear cache of data, queue, filter
            await _dataManager.Clear(job.Id);
            await _queueManager.Clear();
            await _filterManager.Clear();

            _crawlCancellationTokenProvider.DisposeOfToken(job.Id);
        }

        private async Task<List<CompletedJobData>> GetData(string jobId, CompletedJob completedJob)
        {
            var raw = await _dataManager.GetData(jobId);

            var result = new List<CompletedJobData>();

            if (raw == null)
            {
                return result;
            }
            
            foreach (var (key, value) in raw)
            {
                result
                    .AddRange(
                        value
                            .Select(
                                item => new CompletedJobData
                                {
                                    Source = key.AbsoluteUri, 
                                    Value = item, 
                                    CompletedJobId = completedJob.Id,
                                    CompletedJob = completedJob
                                }));
            }

            return result;
        }
    }
}