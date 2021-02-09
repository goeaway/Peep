﻿using Peep.BrowserAdapter;
using Peep.Core;
using Peep.Core.BrowserAdapter;
using Peep.Exceptions;
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
        }

        public Task<CrawlResult> Crawl(CrawlJob job, CancellationToken cancellationToken) 
            => Crawl(job, TimeSpan.MinValue, null, cancellationToken);

        public Task<CrawlResult> Crawl(CrawlJob job,
            TimeSpan progressUpdateTime,
            Action<CrawlProgress> progressUpdate,
            CancellationToken cancellationToken) 
            => Crawl(job, progressUpdateTime, p => Task.Run(() => progressUpdate(p)), cancellationToken);

        public async Task<CrawlResult> Crawl(CrawlJob job, TimeSpan progressUpdateTime,
            Func<CrawlProgress, Task> progressUpdate, CancellationToken cancellationToken)
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
                    data,
                    browserAdapter,
                    userAgent,
                    cancellationToken,
                    stopwatch,
                    progressUpdateTime,
                    progressUpdate);
            }

            stopwatch.Stop();
            return new CrawlResult { CrawlCount = _crawlerOptions.Filter.Count, Data = data, Duration = stopwatch.Elapsed };
        }

        private async Task InnerCrawl(
            CrawlJob job, 
            ConcurrentQueue<Uri> queue, 
            Dictionary<Uri, IEnumerable<string>> data,
            IBrowserAdapter browserAdapter, 
            string userAgent,
            CancellationToken cancellationToken,
            Stopwatch stopwatch,
            TimeSpan progressUpdateTime,
            Func<CrawlProgress, Task> progressUpdate)
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
                    CrawlCount = _crawlerOptions.Filter.Count,
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
                    await progressUpdate.Invoke(progress);
                    progressStopwatch.Restart();
                }

                // get next URI 
                queue.TryDequeue(out var next);
                // if no next or filter already contains it, continue
                if (next == null || _crawlerOptions.Filter.Contains(next.AbsoluteUri))
                {
                    continue;
                }

                var response = await browserAdapter.NavigateToAsync(next);

                if(response && !cancellationToken.IsCancellationRequested)
                {
                    // perform any page actions to get the page in a certain state before extracting content
                    if(job.PageActions != null && job.PageActions.Any())
                    {
                        foreach(var paction in job.PageActions)
                        {
                            // only perform if the action if the page action doesn't have a uri regex, or it matches the current page
                            if(string.IsNullOrWhiteSpace(paction.UriRegex) || Regex.IsMatch(next.AbsoluteUri, paction.UriRegex))
                            {
                                await paction.Perform(browserAdapter);
                            }
                        }
                    }

                    var content = await browserAdapter.GetContentAsync();
                    _crawlerOptions.Filter.Add(next.AbsoluteUri);

                    // extract URIs and data from content
                    await ExtractData(
                        content,
                        next,
                        queue,
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
                    && !_crawlerOptions.Filter.Contains(link.AbsoluteUri)
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