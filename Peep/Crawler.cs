using Peep.Abstractions;
using Peep.Filtering;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peep
{
    public class Crawler : ICrawler
    {
        private readonly CrawlerOptions _crawlerOptions;

        public Crawler() : this(new CrawlerOptions()) { }

        public Crawler(CrawlerOptions options)
        {
            _crawlerOptions = options;
        }

        public Task<CrawlResult> Crawl(Uri seed, CancellationToken cancellationToken) 
            => Crawl(seed, new CrawlOptions(), cancellationToken);
        public Task<CrawlResult> Crawl(Uri seed, CrawlOptions options, CancellationToken cancellationToken) 
            => Crawl(new List<Uri> { seed }, options, cancellationToken);
        public Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CancellationToken cancellationToken) 
            => Crawl(seeds, new CrawlOptions(), cancellationToken);

        public async Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CrawlOptions options, CancellationToken cancellationToken)
        {
            var queue = new ConcurrentQueue<Uri>(seeds);
            var filter = new BloomFilter(100000);
            var data = new Dictionary<Uri, IEnumerable<string>>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _crawlerOptions.DataExtractor.LoadCustomRegexPattern(options.DataRegex);

            using(var browser = await _crawlerOptions.BrowserFactory.GetBrowser())
            {
                var innerCancellationTokenSource = new CancellationTokenSource();
                var userAgent = await browser.GetUserAgentAsync();

                var tasks = Enumerable
                    .Range(0, (int)_crawlerOptions.Threads)
                    .Select(i => ThreadWork(
                            i,
                            options,
                            queue,
                            filter,
                            data,
                            browser,
                            userAgent,
                            innerCancellationTokenSource,
                            stopwatch));

                await Task.WhenAll(tasks);
            }

            stopwatch.Stop();
            return new CrawlResult { CrawlCount = filter.Count, Data = data, Duration = stopwatch.Elapsed };
        }

        private async Task ThreadWork(
            int threadId,
            CrawlOptions options, 
            ConcurrentQueue<Uri> queue, 
            BloomFilter filter,
            Dictionary<Uri, IEnumerable<string>> data,
            Browser browser, 
            string userAgent,
            CancellationTokenSource cancellationTokenSource,
            Stopwatch stopwatch)
        {
            using (var page = await browser.NewPageAsync())
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    // first thread checks for stop conditions
                    if(threadId == 0)
                    {
                        // check stop conditions
                        // if stop should happen, set cancellation
                        var progress = new CrawlProgress
                        {
                            CrawlCount = filter.Count,
                            DataCount = data.Count,
                            Duration = stopwatch.Elapsed
                        };

                        var shouldStop = options.StopConditions.Any(sc => sc.Stop(progress));

                        if (shouldStop)
                        {
                            cancellationTokenSource.Cancel();
                        }
                    }

                    var next = DequeueOrRetry(queue, cancellationTokenSource.Token);

                    if (filter.Contains(next.AbsoluteUri))
                    {
                        continue;
                    }

                    filter.Add(next.AbsoluteUri);

                    var response = await page.GoToAsync(next.AbsoluteUri);
                    // check after long awaits before starting more long tings
                    if (response.Ok && !cancellationTokenSource.IsCancellationRequested)
                    {
                        // parse
                        var content = await page.GetContentAsync();

                        var primedNext = !next.AbsolutePath.EndsWith("/")
                            ? next.AbsolutePath + "/"
                            : next.AbsolutePath;

                        foreach (var link in _crawlerOptions.DataExtractor.ExtractURIs(next, content))
                        {
                            if(cancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            // must be from the same place as the crawled link,
                            // must not have been crawled already,
                            if (link.Host == next.Host
                                && link.AbsolutePath.Contains(primedNext)
                                && !filter.Contains(link.AbsoluteUri)
                                && (options.IgnoreRobots || !await _crawlerOptions.RobotParser.UriForbidden(link, userAgent)))
                            {
                                queue.Enqueue(link);
                            }
                        }

                        if(!cancellationTokenSource.IsCancellationRequested)
                        {
                            var pageData = _crawlerOptions.DataExtractor.ExtractData(content);
                            data.Add(next, pageData);
                        }
                    }
                }
            }
        }

        private Uri DequeueOrRetry(ConcurrentQueue<Uri> queue, CancellationToken cancellationToken)
        {
            Uri next;
            while (!queue.TryDequeue(out next) && !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(10);
            }

            return next;
        }
    }
}
