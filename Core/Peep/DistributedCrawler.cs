using Peep.BrowserAdapter;
using Peep.Exceptions;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Peep.Data;
using Polly;
using Peep.Filtering;
using Peep.Queueing;

namespace Peep
{
    public class DistributedCrawler : ICrawler
    {
        private readonly CrawlerOptions _crawlerOptions;

        public DistributedCrawler() : this(new CrawlerOptions()) { }

        public DistributedCrawler(CrawlerOptions options)
        {
            _crawlerOptions = options ?? throw new ArgumentNullException(nameof(options));

            if(options.BrowserAdapterFactory == null)
            {
                throw new CrawlerOptionsException("Browser adapter factory required");
            }

            if(options.DataExtractor == null)
            {
                throw new CrawlerOptionsException("Data extractor required");
            }

            if(options.RobotParser == null)
            {
                throw new CrawlerOptionsException("Robot Parser required");
            }

            if (options.Logger == null)
            {
                throw new CrawlerOptionsException("Logger required");
            }
        }

        public ChannelReader<CrawlProgress> Crawl(
            CrawlJob job, 
            int dataCountUpdate, 
            ICrawlFilter filter,
            ICrawlQueue queue,
            CancellationToken cancellationToken)
        {
            if(job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (job.Seeds?.Count() == 0)
            {
                throw new InvalidOperationException("at least one seed URI is required");
            }

            if(filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if(queue == null)
            {
                throw new ArgumentNullException(nameof(queue));
            }

            if(dataCountUpdate < 1)
            {
                throw new InvalidOperationException(
                    $"Minimum data count update of 1 required, {dataCountUpdate} was provided.");
            }

            var channel = Channel.CreateUnbounded<CrawlProgress>(new UnboundedChannelOptions 
            { 
                AllowSynchronousContinuations = true, // extremely important
                SingleReader = true,
                SingleWriter = true
            });
            
            var data = new ExtractedData();

            _crawlerOptions.DataExtractor.LoadCustomRegexPattern(job.DataRegex);

            // have a task in here that starts the crawl, and then when we get to the end of that set the channel as complete
            // fire and forget task?
                
            _ = Task.Run(async () =>
            {
                try
                {
                    using var browserAdapter = await _crawlerOptions.BrowserAdapterFactory.GetBrowserAdapter();
                    var userAgent = await browserAdapter.GetUserAgentAsync();
                    
                    await InnerCrawl(
                        job,
                        data,
                        browserAdapter,
                        userAgent,
                        filter,
                        queue,
                        cancellationToken,
                        dataCountUpdate,
                        channel.Writer);
                }
                catch (Exception e) when (e is TaskCanceledException || e is OperationCanceledException)
                {
                    // ignore
                    await channel.Writer.WriteAsync(new CrawlProgress { Data = data }, CancellationToken.None);
                    channel.Writer.Complete();
                }
                catch (Exception e)
                {
                    channel.Writer.Complete(new CrawlerRunException(
                        e.Message,
                        new CrawlProgress {Data = data},
                        e));
                }
            }, cancellationToken);

            return channel.Reader;
        }

        private async Task InnerCrawl(
            CrawlJob job, 
            ExtractedData data,
            IBrowserAdapter browserAdapter, 
            string userAgent,
            ICrawlFilter filter,
            ICrawlQueue queue,
            CancellationToken cancellationToken,
            int dataCountUpdate,
            ChannelWriter<CrawlProgress> channelWriter)
        {
            var pageActionRetryPolicy = Policy
                .Handle<WaitTaskTimeoutException>()
                .WaitAndRetryAsync(
                    _crawlerOptions.PageActionRetryCount, 
                    attempt => TimeSpan.FromMilliseconds(attempt * 200));

            var pageAdapterTasks = browserAdapter
                .GetPageAdapters()
                .Select(Task.FromResult)
                .ToList();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                if(data.Count >= dataCountUpdate)
                {
                    if(channelWriter.TryWrite(new CrawlProgress { Data = new ExtractedData(data) }))
                    {
                        data.Clear();
                    }
                }

                if (!pageAdapterTasks.Any())
                {
                    await Task.Delay(50, cancellationToken);
                    continue;
                }
                
                var next = await queue.Dequeue();

                if (next == null || await filter.Contains(next.AbsoluteUri))
                {
                    await Task.Delay(50, cancellationToken);
                    continue;
                }

                // TODO send dequeue heartbeat?
                
                await filter.Add(next.AbsoluteUri);
                
                var availablePageAdapterTask = await Task.WhenAny(pageAdapterTasks);
                pageAdapterTasks.Remove(availablePageAdapterTask);

                var availablePageAdapter = await availablePageAdapterTask;

                pageAdapterTasks.Add(Task
                    .Run(async () =>
                    {
                        var response = await availablePageAdapter
                            .NavigateToAsync(next);

                        if (!response || cancellationToken.IsCancellationRequested)
                        {
                            return availablePageAdapter;
                        }

                        // perform any page actions to get the page in a certain state before extracting content
                        if (job.PageActions != null && job.PageActions.Any())
                        {
                            foreach (var pageAction in job.PageActions)
                            {
                                // only perform if the action of the page action doesn't have a uri regex, or it matches the current page
                                if ((string.IsNullOrWhiteSpace(pageAction.UriRegex) ||
                                     Regex.IsMatch(next.AbsoluteUri, pageAction.UriRegex)) &&
                                    !cancellationToken.IsCancellationRequested)
                                {
                                    // retry here
                                    await pageActionRetryPolicy
                                        .ExecuteAsync(
                                            cT => _crawlerOptions.PageActionPerformer.Perform(pageAction,
                                                availablePageAdapter), cancellationToken);
                                }
                            }
                        }

                        var content = await availablePageAdapter.GetContentAsync();

                        // extract URIs and data from content
                        await ExtractData(
                            content,
                            next,
                            data,
                            job,
                            userAgent,
                            filter,
                            queue,
                            cancellationToken);
                        
                        return availablePageAdapter;
                }, cancellationToken));
            }
        }

        private async Task ExtractData(
            string content,
            Uri currentUri, 
            IDictionary<Uri, IEnumerable<string>> data,
            CrawlJob job,
            string userAgent,
            ICrawlFilter filter,
            ICrawlQueue queue,
            CancellationToken cancellationToken)
        {
            var primedNext = !currentUri.AbsolutePath.EndsWith("/")
                        ? currentUri.AbsolutePath + "/"
                        : currentUri.AbsolutePath;

            foreach (var link in _crawlerOptions.DataExtractor.ExtractURIs(currentUri, content))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // must be from the same place as the crawled link, or matches the optional uri regex,
                // must not have been crawled already,
                // must not be blocked by robots.txt
                if (link.Host == currentUri.Host
                    && (string.IsNullOrWhiteSpace(job.UriRegex) ? link.AbsolutePath.Contains(primedNext) : Regex.IsMatch(link.AbsoluteUri, job.UriRegex))
                    && !await filter.Contains(link.AbsoluteUri)
                    && (job.IgnoreRobots || !await _crawlerOptions.RobotParser.UriForbidden(link, userAgent)))
                {
                    await queue.Enqueue(link);
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                var pageData = _crawlerOptions.DataExtractor.ExtractData(content);

                if(pageData.Any())
                {
                    data.Add(currentUri, pageData);
                }
            }
        }
    }
}
