using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Paramore.Brighter;
using Peep.API.Application.Options;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core;
using Peep.Core.API.Messages;
using Peep.Core.API.Providers;
using Peep.Exceptions;
using RabbitMQ.Client;
using Serilog;

namespace Peep.API.Application.Services
{
    public class CrawlerManagerService : BackgroundService
    {
        private PeepApiContext _context;
        private ILogger _logger;
        private INowProvider _nowProvider;
        private ICrawlCancellationTokenProvider _crawlCancellationTokenProvider;
        private IAmACommandProcessor _commandProcessor;

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
            IAmACommandProcessor commandProcessor)
        {
            _logger = logger;
            _context = context;
            _nowProvider = nowProvider;
            _crawlCancellationTokenProvider = crawlCancellationTokenProvider;
            _commandProcessor = commandProcessor;
        }

        private IServiceScope SetServices(IServiceScope serviceScope)
        {
            if(serviceScope != null)
            {
                _logger = serviceScope.ServiceProvider.GetRequiredService<ILogger>();
                _nowProvider = serviceScope.ServiceProvider.GetRequiredService<INowProvider>();
                _context = serviceScope.ServiceProvider.GetRequiredService<PeepApiContext>();
                _crawlCancellationTokenProvider = serviceScope.ServiceProvider.GetRequiredService<ICrawlCancellationTokenProvider>();
                _commandProcessor = serviceScope.ServiceProvider.GetRequiredService<IAmACommandProcessor>();
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
                    _context.RunningJobs.Add(new RunningJob
                    {
                        Id = job.Id,
                        JobJson = job.JobJson,
                        DateQueued = job.DateQueued,
                        DateStarted = _nowProvider.Now
                    });

                    _context.SaveChanges();

                    var duration = new Stopwatch();
                    duration.Start();

                    var stoppableCrawlJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(job.JobJson);
                    // add job seeds to queue
                    // broadcast job start to crawlers

                    var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                        stoppingToken, _crawlCancellationTokenProvider.GetToken(job.Id));

                    var crawlJob = stoppableCrawlJob as CrawlJob;
                    var identifiableCrawlJob = crawlJob as IdentifiableCrawlJob;
                    identifiableCrawlJob.Id = job.Id;

                    await _commandProcessor
                        .PostAsync(
                            new QueueCrawlMessage(identifiableCrawlJob), 
                            cancellationToken: combinedCancellationTokenSource.Token);

                    // go into loop checking for job result + cancellation
                    while (!combinedCancellationTokenSource.IsCancellationRequested)
                    {
                        // assess duration, crawl count from filter
                        // and total data (crawlers should provide easily accessible count of data in each 
                        // push, so we can aggregate easier without having to serialise the data here each time)
                        // check that info against stop conditions
                        var result = new CrawlResult
                        {
                            CrawlCount = 1,

                            Duration = duration.Elapsed,
                        };

                        if(stoppableCrawlJob.StopConditions.Any(sc => sc.Stop(result)))
                        {
                            combinedCancellationTokenSource.Cancel();
                            break;
                        }

                        await Task.Delay(500, combinedCancellationTokenSource.Token);
                    }

                    // the only way to be here is if the token source is cancelled
                    // so broadcast cancellation to crawlers
                    // give crawlers a chance to finish up and respond somehow (maybe an EOF push in the data cache?)
                    await _commandProcessor.PostAsync(
                        new CancelCrawlMessage(job.Id),
                        cancellationToken: combinedCancellationTokenSource.Token);

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
