using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using Peep.Abstractions;
using Peep.Filtering;
using PuppeteerSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Peep
{
    public class Crawler : ICrawler
    {
        private readonly CrawlerOptions _crawlerOptions;

        public Crawler() : this(new CrawlerOptions()) { }

        public Crawler(CrawlerOptions options)
        {
            _crawlerOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<CrawlResult> Crawl(Uri seed, CancellationToken cancellationToken) 
            => Crawl(seed, new CrawlOptions(), cancellationToken);
        public Task<CrawlResult> Crawl(Uri seed, CrawlOptions options, CancellationToken cancellationToken) 
            => Crawl(new List<Uri> { seed }, options, cancellationToken);
        public Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CancellationToken cancellationToken) 
            => Crawl(seeds, new CrawlOptions(), cancellationToken);

        public async Task<CrawlResult> Crawl(IEnumerable<Uri> seeds, CrawlOptions options, CancellationToken cancellationToken)
        {
            if(seeds == null)
            {
                throw new ArgumentNullException(nameof(seeds));
            }

            if(seeds.Count() == 0)
            {
                throw new ArgumentException("at least one seed URI is required");
            }

            if(options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if(cancellationToken == null)
            {
                throw new ArgumentNullException(nameof(cancellationToken));
            }

            var queue = new ConcurrentQueue<Uri>(seeds);
            var filter = new BloomFilter(100000);
            var data = new Dictionary<Uri, IEnumerable<string>>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _crawlerOptions.DataExtractor.LoadCustomRegexPattern(options.DataRegex);

            using(var browser = await _crawlerOptions.BrowserFactory.GetBrowser())
            {
                var userAgent = await browser.GetUserAgentAsync();

                await InnerCrawl(
                    options,
                    queue,
                    filter,
                    data,
                    browser,
                    userAgent,
                    cancellationToken,
                    stopwatch);
            }

            stopwatch.Stop();
            return new CrawlResult { CrawlCount = filter.Count, Data = data, Duration = stopwatch.Elapsed };
        }

        private async Task InnerCrawl(
            CrawlOptions options, 
            ConcurrentQueue<Uri> queue, 
            BloomFilter filter,
            Dictionary<Uri, IEnumerable<string>> data,
            Browser browser, 
            string userAgent,
            CancellationToken cancellationToken,
            Stopwatch stopwatch)
        {
            var checkConditions = options.StopConditions != null && options.StopConditions.Any();
            var page = (await browser.PagesAsync()).First();
            var waitStopwatch = new Stopwatch();

            while (!cancellationToken.IsCancellationRequested)
            {
                // first thread checks for stop conditions
                if (checkConditions)
                {
                    // check stop conditions
                    // if stop should happen, set break
                    var progress = new CrawlProgress
                    {
                        CrawlCount = filter.Count,
                        DataCount = data.Count,
                        Duration = stopwatch.Elapsed
                    };

                    var shouldStop = options.StopConditions.Any(sc => sc.Stop(progress));

                    if (shouldStop)
                    {
                        break;
                    }
                }

                if(cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // get next URI
                queue.TryDequeue(out var next);
                // if no next or filter already contains it, continue
                if (next == null || filter.Contains(next.AbsoluteUri))
                {
                    continue;
                }

                var response = await page.GoToAsync(next.AbsoluteUri, WaitUntilNavigation.DOMContentLoaded);

                if(response.Ok && !cancellationToken.IsCancellationRequested)
                {
                    // if crawl options contain wait options
                    if(!string.IsNullOrWhiteSpace(options.WaitOptions?.Selector) && options.WaitOptions?.MillisecondsTimeout > 0)
                    {
                        waitStopwatch.Start();
                        // while we haven't timed out or been told to cancel
                        while (
                            !(await page.QuerySelectorAllAsync(options.WaitOptions.Selector)).Any() &&
                            waitStopwatch.ElapsedMilliseconds < options.WaitOptions.MillisecondsTimeout &&
                            !cancellationToken.IsCancellationRequested)
                        {
                            Thread.Sleep(10);
                        }
                        waitStopwatch.Reset();
                    }

                    var content = await page.GetContentAsync();
                    filter.Add(next.AbsoluteUri);

                    // extract URIs and data from content
                    await ExtractData(
                        content,
                        next,
                        queue,
                        filter,
                        data,
                        options,
                        userAgent,
                        cancellationToken
                    );
                }
            }
        }

        private async Task ExtractData(
            string content,
            Uri currentUri, 
            ConcurrentQueue<Uri> queue,
            BloomFilter filter,
            Dictionary<Uri, IEnumerable<string>> data,
            CrawlOptions options,
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
                    && (string.IsNullOrWhiteSpace(options.UriRegex) ? link.AbsolutePath.Contains(primedNext) : Regex.IsMatch(link.AbsoluteUri, options.UriRegex))
                    && !filter.Contains(link.AbsoluteUri)
                    && (options.IgnoreRobots || !await _crawlerOptions.RobotParser.UriForbidden(link, userAgent)))
                {
                    queue.Enqueue(link);
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
