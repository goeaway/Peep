using Peep.BrowserAdapter;
using Peep.Exceptions;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Polly;
using Peep.Filtering;
using Peep.Queueing;

namespace Peep
{
    public class DistributedCrawler : ICrawler
    {
        private readonly CrawlerOptions _crawlerOptions;

        private static readonly CrawlerOptions DefaultCrawlerOptions = new CrawlerOptions
        {
            Filter = new BloomFilter(1_000_000),
            Queue = new CrawlQueue()
        };

        public DistributedCrawler() : this(DefaultCrawlerOptions) { }

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

            if(options.Filter == null)
            {
                throw new CrawlerOptionsException("Filter required");
            }

            if(options.Queue == null)
            {
                throw new CrawlerOptionsException("Queue required");
            }
        }

        public ChannelReader<CrawlProgress> Crawl(CrawlJob job, TimeSpan channelUpdateTimeSpan, CancellationToken cancellationToken)
        {
            if(job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if (job.Seeds?.Count() == 0)
            {
                throw new InvalidOperationException("at least one seed URI is required");
            }

            var channel = Channel.CreateUnbounded<CrawlProgress>(new UnboundedChannelOptions 
            { 
                AllowSynchronousContinuations = true, // extremely important
                SingleReader = true,
                SingleWriter = true
            });

            // have a task in here that starts the crawl, and then when we get to the end of that set the channel as complete
            // fire and forget task?
            Task.Run(async () =>
            {
                var data = new Dictionary<Uri, IEnumerable<string>>();

                _crawlerOptions.DataExtractor.LoadCustomRegexPattern(job.DataRegex);

                using (var browserAdapter = await _crawlerOptions.BrowserAdapterFactory.GetBrowserAdapter())
                {
                    var userAgent = await browserAdapter.GetUserAgentAsync();

                    try
                    {
                        await InnerCrawl(
                            job,
                            data,
                            browserAdapter,
                            userAgent,
                            cancellationToken,
                            channel.Writer, 
                            channelUpdateTimeSpan);
                    }
                    catch (Exception e)
                    {
                        channel.Writer.Complete(new CrawlerRunException(
                            e.Message,
                            new CrawlProgress { Data = data },
                            e));
                    }
                }

                await channel.Writer.WriteAsync(new CrawlProgress { Data = data });
                data.Clear();
                channel.Writer.Complete();
            });

            return channel.Reader;
        }

        private async Task InnerCrawl(
            CrawlJob job, 
            Dictionary<Uri, IEnumerable<string>> data,
            IBrowserAdapter browserAdapter, 
            string userAgent,
            CancellationToken cancellationToken,
            ChannelWriter<CrawlProgress> channelWriter = null,
            TimeSpan channelWriterUpdateTime = default)
        {
            var progressStopwatch = new Stopwatch();
            progressStopwatch.Start();

            var pageActionRetryPolicy = Policy
                .Handle<WaitTaskTimeoutException>()
                .WaitAndRetryAsync(_crawlerOptions.PageActionRetryCount, attempt => TimeSpan.FromMilliseconds(attempt * 200));

            var queueEmptyRetryPolicy = Policy
                .HandleResult<Uri>(uri => uri == null)
                .WaitAndRetryAsync(_crawlerOptions.QueueEmptyRetryCount, attempt => TimeSpan.FromMilliseconds(attempt * 200));

            while (!cancellationToken.IsCancellationRequested)
            {
                if(progressStopwatch.Elapsed >= channelWriterUpdateTime)
                {
                    await channelWriter.WriteAsync(new CrawlProgress { Data = data });
                    data.Clear();
                    progressStopwatch.Restart();
                }

                // get next uri, if this returns null we will retry using the retry policy
                var next = await queueEmptyRetryPolicy.ExecuteAsync(cT => _crawlerOptions.Queue.Dequeue(), cancellationToken);

                // after a certain amount of retries the policy will just allow through anyway, we then handle below
                if (next == null) 
                {
                    // don't throw, just break
                    if(cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    throw new 
                        CrawlerEmptyQueueRetryLimitExceededException(
                            $"Queue was empty {_crawlerOptions.QueueEmptyRetryCount} time(s)");
                }

                // if filter contains next already we continue
                if (await _crawlerOptions.Filter.Contains(next.AbsoluteUri))
                {
                    continue;
                }

                var response = await browserAdapter.NavigateToAsync(next);

                if(response && !cancellationToken.IsCancellationRequested)
                {
                    await _crawlerOptions.Filter.Add(next.AbsoluteUri);

                    // perform any page actions to get the page in a certain state before extracting content
                    if(job.PageActions != null && job.PageActions.Any())
                    {
                        foreach(var paction in job.PageActions)
                        {
                            // only perform if the action if the page action doesn't have a uri regex, or it matches the current page
                            if((string.IsNullOrWhiteSpace(paction.UriRegex) || Regex.IsMatch(next.AbsoluteUri, paction.UriRegex)) && !cancellationToken.IsCancellationRequested)
                            {
                                // retry here
                                await pageActionRetryPolicy
                                    .ExecuteAsync(cT => paction.Perform(browserAdapter), cancellationToken);
                            }
                        }
                    }

                    var content = await browserAdapter.GetContentAsync();

                    // extract URIs and data from content
                    await ExtractData(
                        content,
                        next,
                        data,
                        job,
                        userAgent,
                        cancellationToken
                    );
                }
            }
        }

        private async Task ExtractData(
            string content,
            Uri currentUri, 
            Dictionary<Uri, IEnumerable<string>> data,
            CrawlJob job,
            string userAgent,
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
                    && !await _crawlerOptions.Filter.Contains(link.AbsoluteUri)
                    && (job.IgnoreRobots || !await _crawlerOptions.RobotParser.UriForbidden(link, userAgent)))
                {
                    await _crawlerOptions.Queue.Enqueue(link);
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
