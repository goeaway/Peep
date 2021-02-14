using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Peep.Core.API.Providers;
using Peep.Exceptions;
using Peep.Crawler.Application.Options;
using Peep.Crawler.Application.Providers;
using Serilog;

namespace Peep.Crawler.Application.Services
{
    public class CrawlerRunnerService : BackgroundService
    {
        private ILogger _logger;
        private ICrawler _crawler;
        private INowProvider _nowProvider;
        private CrawlConfigOptions _options;
        private ICrawlCancellationTokenProvider _tokenProvider;

        private readonly IServiceProvider _serviceProvider;

        public CrawlerRunnerService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public CrawlerRunnerService(
            ILogger logger,
            ICrawler crawler,
            INowProvider nowProvider,
            CrawlConfigOptions options,
            ICrawlCancellationTokenProvider tokenProvider)
        {
            _logger = logger;
            _crawler = crawler;
            _nowProvider = nowProvider;
            _options = options;
            _tokenProvider = tokenProvider;
        }

        private IServiceScope SetServices(IServiceScope serviceScope)
        {
            if(serviceScope != null)
            {
                _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger>();
                _crawler = serviceScope.ServiceProvider.GetRequiredService<ICrawler>();
                _nowProvider = serviceScope.ServiceProvider.GetRequiredService<INowProvider>();
                _options = serviceScope.ServiceProvider.GetRequiredService<CrawlConfigOptions>();
                _tokenProvider = serviceScope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
            }
            
            return serviceScope;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = SetServices(_serviceProvider?.CreateScope());
            _logger.Information("Waiting for jobs");

            while (!stoppingToken.IsCancellationRequested)
            {
                // try get job from repository
                var jobFound = false;

                if (jobFound)
                {
                    // enrich logs in here with job id
                    var crawlJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(null);

                    var startTime = _nowProvider.Now;

                    // create a linked token source so we can stop the crawl if the service needs to stop
                    // or the job needs to stop
                    var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken,
                        _tokenProvider.GetToken(null));
                    
                    try
                    {
                        var channelReader = _crawler.Crawl(
                            crawlJob,
                            TimeSpan.FromMilliseconds(_options.ProgressUpdateMilliseconds),
                            cancellationTokenSource.Token);

                        // async iterate over channel's results
                        // update the running jobs running totals of the crawl result
                        await foreach (var result in channelReader.ReadAllAsync(cancellationTokenSource.Token))
                        {
                            // find running in cache
                            //var rJ = await _runningCrawlJobRepository.GetRunningJob(queuedJob.Id);

                            //// update values
                            //rJ.DataJson = JsonConvert.SerializeObject(result.Data);

                            //// save 
                            //await _runningCrawlJobRepository.SaveJob(rJ);
                        }

                        _logger.Information("Saving result");

                        //_context.CompletedJobs.Add(new CompletedJob
                        //{
                        //    JobJson = queuedJob.JobJson,
                        //    Id = queuedJob.Id,
                        //    CrawlCount = runningJob.CrawlCount,
                        //    DataJson = runningJob.DataJson,
                        //    Duration = runningJob.Duration,
                        //    DateQueued = queuedJob.DateQueued,
                        //    DateStarted = startTime,
                        //    DateCompleted = _nowProvider.Now,
                        //});
                    }
                    catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                    {
                        //var runningJob = await _runningCrawlJobRepository.GetRunningJob(queuedJob.Id);

                        //// build a completed job record based off the running job values
                        //_context.CompletedJobs.Add(new CompletedJob
                        //{
                        //    JobJson = queuedJob.JobJson,
                        //    Id = queuedJob.Id,
                        //    CrawlCount = runningJob.CrawlCount,
                        //    DataJson = runningJob.DataJson,
                        //    Duration = runningJob.Duration,
                        //    DateQueued = queuedJob.DateQueued,
                        //    DateStarted = startTime,
                        //    DateCompleted = _nowProvider.Now,
                        //});
                    }
                    catch (CrawlerRunException e)
                    {
                        //_context.ErroredJobs.Add(new ErroredJob
                        //{
                        //    Id = queuedJob.Id,
                        //    ErrorMessage = e.Message,
                        //    JobJson = queuedJob.JobJson,
                        //    DateStarted = startTime,
                        //    DateCompleted = _nowProvider.Now,
                        //    DateQueued = queuedJob.DateQueued,
                        //    DataJson = JsonConvert.SerializeObject(e.CrawlProgress.Data),
                        //});
                    }
                    catch (Exception e)
                    {
                        //_context.ErroredJobs.Add(new ErroredJob
                        //{
                        //    Id = queuedJob.Id,
                        //    ErrorMessage = e.Message,
                        //    JobJson = queuedJob.JobJson,
                        //    DateStarted = startTime,
                        //    DateCompleted = _nowProvider.Now,
                        //    DateQueued = queuedJob.DateQueued,
                        //});
                    }
                    finally
                    {
                        //await _runningCrawlJobRepository.RemoveJob(queuedJob.Id);
                        //_tokenProvider.DisposeOfToken(queuedJob.Id);

                        //_context.SaveChanges();
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
