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
using Peep.Crawler.Infrastructure;
using Peep.Crawler.Models.DTOs;
using Peep.Queueing;
using Peep.Filtering;

namespace Peep.Crawler.Application.Services
{
    public class CrawlerRunnerService : BackgroundService
    {
        private ILogger _logger;
        private ICrawler _crawler;
        private INowProvider _nowProvider;
        private CrawlConfigOptions _options;
        private ICrawlCancellationTokenProvider _tokenProvider;
        private IGenericRepository<CrawlDataDTO> _crawlDataRepository;
        private ICrawlFilter _filter;
        private ICrawlQueue _queue;

        private readonly IServiceProvider _serviceProvider;

        public CrawlerRunnerService(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public CrawlerRunnerService(
            CrawlConfigOptions options,
            ILogger logger,
            ICrawler crawler,
            INowProvider nowProvider,
            ICrawlFilter filter,
            ICrawlQueue queue,
            ICrawlCancellationTokenProvider tokenProvider,
            IGenericRepository<CrawlDataDTO> crawlDataRepository)
        {
            _logger = logger;
            _crawler = crawler;
            _nowProvider = nowProvider;
            _options = options;
            _tokenProvider = tokenProvider;
            _crawlDataRepository = crawlDataRepository;
            _filter = filter;
            _queue = queue;
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
                _filter = serviceScope.ServiceProvider.GetRequiredService<ICrawlFilter>();
                _queue = serviceScope.ServiceProvider.GetRequiredService<ICrawlQueue>();
                _crawlDataRepository = serviceScope.ServiceProvider.GetRequiredService<IGenericRepository<CrawlDataDTO>>();
            }
            
            return serviceScope;
        }

        private string KeyGenerator(string jobId)
        {
            return $"{jobId}.{Guid.NewGuid()}";
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
                    var crawlJob = JsonConvert.DeserializeObject<CrawlJob>(null);

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
                            _filter,
                            _queue,
                            cancellationTokenSource.Token);

                        try
                        {
                            // async iterate over channel's results
                            // update the running jobs running totals of the crawl result
                            await foreach (var result in channelReader.ReadAllAsync(cancellationTokenSource.Token))
                            {
                                // create a unique key for each push, the crawler manager will then combine all 
                                // data pushes for the same job together when the crawl is completed
                                await _crawlDataRepository.Set(KeyGenerator(""), new CrawlDataDTO
                                {
                                    Count = result.Data.Count,
                                    Data = result.Data
                                });
                            }
                        }
                        catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException) // cancellation token for channel reader causes this
                        {
                            // occurs when cancellation occurs, so we can ignore and treat as normal
                        }

                        // send EOC data push to show this crawler is complete
                        await _crawlDataRepository.Set(KeyGenerator(""), new CrawlDataDTO
                        {
                            Complete = true
                        });
                    }
                    catch (CrawlerRunException e)
                    {
                        await _crawlDataRepository.Set(KeyGenerator(""), new CrawlDataDTO
                        {
                            Complete = true,
                            Data = e.CrawlProgress.Data,
                            ErrorMessage = e.Message
                        });
                    }
                    catch (Exception e)
                    {
                        await _crawlDataRepository.Set(KeyGenerator(""), new CrawlDataDTO
                        {
                            Complete = true,
                            ErrorMessage = e.Message
                        });
                    }
                    finally
                    {
                        _tokenProvider.DisposeOfToken(null);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
