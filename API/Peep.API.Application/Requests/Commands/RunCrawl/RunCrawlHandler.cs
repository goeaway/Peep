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
using Peep.Core.Infrastructure.Filtering;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
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
        private readonly ICrawlFilterManager _filterManager;
        private readonly ICrawlQueueManager _queueManager;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ICrawlerManager _crawlerManager;
        
        public RunCrawlHandler(
            PeepApiContext context, 
            ILogger logger, 
            INowProvider nowProvider, 
            ICrawlCancellationTokenProvider crawlCancellationTokenProvider, 
            ICrawlFilterManager filterManager, 
            ICrawlQueueManager queueManager, 
            IPublishEndpoint publishEndpoint, 
            ICrawlerManager crawlerManager)
        {
            _context = context;
            _logger = logger;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;

            _filterManager = filterManager;
            _queueManager = queueManager;
            _publishEndpoint = publishEndpoint;
            _crawlerManager = crawlerManager;
        }
        
        public async Task<Unit> Handle(RunCrawlRequest request, CancellationToken cancellationToken)
        {
            using (LogContext.PushProperty("JobId", request.JobId))
            {
                _logger.Information("Running job");
                var stopConditionMet = false;
                
                var foundJob = await _context
                    .Jobs
                    .FindAsync(request.JobId);

                try
                {
                    var cancellationTokenSource = 
                        await StartJob(
                            foundJob, 
                            request.JobActual, 
                            cancellationToken);

                    stopConditionMet = await CheckProgress(
                        foundJob, 
                        request.JobActual, 
                        cancellationTokenSource,
                        foundJob.DateStarted.GetValueOrDefault());
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
                    await CleanupAfterJob(
                        foundJob, 
                        cancellationToken, 
                        stopConditionMet);
                }
                _logger.Information("Finished Job");
            }
            
            return Unit.Value;
        }
        
        private async Task<CancellationTokenSource> StartJob(
            Job job, 
            CrawlJob jobData, 
            CancellationToken stoppingToken)
        {
            await _queueManager.Clear();
            await _filterManager.Clear();
            _crawlerManager.Clear(job.Id);

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
            Job job, 
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
                    // maybe re-introduce data dict here so stop conditions could look for specific
                    // item
                    DataCount = _context.JobData.Count(jd => jd.JobId == job.Id),
                    Duration = _nowProvider.Now - dateStarted
                };

                if (jobData.StopConditions.Any(sc => sc.Stop(result)))
                {
                    _logger.Information("Stop condition reached, stopping crawl");
                    combinedCancellationTokenSource.Cancel();
                    return true;
                }

                var running = await _context.Jobs.FindAsync(job.Id);
                running.CrawlCount = result.CrawlCount;

                await _context.SaveChangesAsync(combinedCancellationTokenSource.Token);

                await Task.Delay(500, combinedCancellationTokenSource.Token);
            }

            return false;
        }

        private async Task CleanupAfterJob(
            Job job, 
            CancellationToken stoppingToken,
            bool stopConditionMet)
        {
            // send cancellation message to stop crawlers
            await _publishEndpoint
                .Publish(
                    new CrawlCancelled
                    {
                        CrawlId = job.Id
                    }, CancellationToken.None);

            var errors = new List<JobError>();
            
            try
            {
                // wait in a loop here until all crawlers involved in job say they're done
                // or timeout happens
                _logger.Information("Waiting for crawlers to complete");
                await _crawlerManager.WaitAllFinished(job.Id, TimeSpan.FromSeconds(20));
            }
            catch (TimeoutException e)
            {
                errors.Add(new JobError()
                {
                    Message =  e.Message, 
                    StackTrace = e.StackTrace, 
                    Source = "API"
                });
            }

            _logger.Information("Saving data");
            // update job with finishing touches

            job.DateCompleted = _nowProvider.Now;
            job.State = stopConditionMet ? JobState.Complete :
                    errors.Any() ? JobState.Errored :
                    JobState.Cancelled;
            job.CrawlCount = await _filterManager.GetCount();

            if (job.JobErrors != null)
            {
                job.JobErrors.AddRange(errors);
            }
            else
            {
                job.JobErrors = errors;
            }
            
            await _context.SaveChangesAsync(stoppingToken);

            // clear cache of data, queue, filter
            await _queueManager.Clear();
            await _filterManager.Clear();
            _crawlerManager.Clear(job.Id);

            _crawlCancellationTokenProvider.DisposeOfToken(job.Id);
        }
    }
}