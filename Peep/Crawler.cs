using Peep.BrowserAdapter;
using Peep.Core;
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

        public Task<CrawlResult> Crawl(CrawlJob job, CancellationToken cancellationToken) 
            => Crawl(job, TimeSpan.MinValue, null, cancellationToken);

        public async Task<CrawlResult> Crawl(CrawlJob job, TimeSpan progressUpdateTime,
            Action<CrawlProgress> progressUpdate, CancellationToken cancellationToken)
        {
            if(job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            if(job.Seeds?.Count() == 0)
            {
                throw new InvalidOperationException("at least one seed URI is required");
            }

            var queue = new ConcurrentQueue<Uri>(job.Seeds);
            var filter = new BloomFilter(1_000_000);
            var data = new Dictionary<Uri, IEnumerable<string>>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _crawlerOptions.DataExtractor.LoadCustomRegexPattern(job.DataRegex);

            using(var browserAdapter = await _crawlerOptions.BrowserAdapterFactory.GetBrowserAdapter())
            {
                var userAgent = await browserAdapter.GetUserAgentAsync();

                await InnerCrawl(
                    job,
                    queue,
                    filter,
                    data,
                    browserAdapter,
                    userAgent,
                    cancellationToken,
                    stopwatch,
                    progressUpdateTime,
                    progressUpdate);
            }

            stopwatch.Stop();
            return new CrawlResult { CrawlCount = filter.Count, Data = data, Duration = stopwatch.Elapsed };
        }

        private async Task InnerCrawl(
            CrawlJob job, 
            ConcurrentQueue<Uri> queue, 
            BloomFilter filter,
            Dictionary<Uri, IEnumerable<string>> data,
            IBrowserAdapter browserAdapter, 
            string userAgent,
            CancellationToken cancellationToken,
            Stopwatch stopwatch,
            TimeSpan progressUpdateTime,
            Action<CrawlProgress> progressUpdate)
        {
            var waitStopwatch = new Stopwatch();
            var progressStopwatch = new Stopwatch();
            progressStopwatch.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                // check stop conditions
                // if stop should happen, set break
                var progress = new CrawlProgress
                {
                    CrawlCount = filter.Count,
                    DataCount = data.Count,
                    Duration = stopwatch.Elapsed
                };

                if(cancellationToken.IsCancellationRequested || (
                    job.StopConditions != null && 
                    job.StopConditions.Any(sc => sc.Stop(progress))))
                {
                    break;
                }

                if(progressUpdate != null && progressStopwatch.ElapsedMilliseconds >= progressUpdateTime.TotalMilliseconds)
                {
                    progressUpdate.Invoke(progress);
                    progressStopwatch.Restart();
                }

                // get next URI 
                queue.TryDequeue(out var next);
                // if no next or filter already contains it, continue
                if (next == null || filter.Contains(next.AbsoluteUri))
                {
                    continue;
                }

                var response = await browserAdapter.NavigateToAsync(next);

                if(response && !cancellationToken.IsCancellationRequested)
                {
                    // if crawl options contain wait options
                    if(!string.IsNullOrWhiteSpace(job.WaitOptions?.Selector) && job.WaitOptions?.MillisecondsTimeout > 0)
                    {
                        waitStopwatch.Start();
                        // while we haven't timed out or been told to cancel
                        while (
                            !(await browserAdapter.QuerySelectorFoundAsync(job.WaitOptions.Selector)) &&
                            waitStopwatch.ElapsedMilliseconds < job.WaitOptions.MillisecondsTimeout &&
                            !cancellationToken.IsCancellationRequested)
                        {
                            Thread.Sleep(10);
                        }
                        waitStopwatch.Reset();
                    }

                    var content = await browserAdapter.GetContentAsync();
                    filter.Add(next.AbsoluteUri);

                    // extract URIs and data from content
                    await ExtractData(
                        content,
                        next,
                        queue,
                        filter,
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
            ConcurrentQueue<Uri> queue,
            BloomFilter filter,
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
                    && !filter.Contains(link.AbsoluteUri)
                    && (job.IgnoreRobots || !await _crawlerOptions.RobotParser.UriForbidden(link, userAgent)))
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
