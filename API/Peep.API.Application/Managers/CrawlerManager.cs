using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Peep.Core.Infrastructure.Messages;

namespace Peep.API.Application.Managers
{
    public class CrawlerManager : ICrawlerManager
    {
        private readonly struct CrawlerState
        {
            public string Id { get; }
            public bool Finished { get; }

            public CrawlerState(string id, bool finished)
            {
                Id = id;
                Finished = finished;
            }
        }
        private readonly IDictionary<string, List<CrawlerState>> _crawlerDict;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        
        public CrawlerManager()
        {
            _crawlerDict = new Dictionary<string, List<CrawlerState>>();
        }

        public void Start(string crawlerId, string jobId)
        {
            _semaphore.Wait();
            try
            {
                if (_crawlerDict.ContainsKey(jobId))
                {
                    _crawlerDict[jobId].Add(new CrawlerState(crawlerId, false));
                }
                else
                {
                    _crawlerDict.Add(jobId, new List<CrawlerState> {new CrawlerState(crawlerId, false)});
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Finish(string crawlerId, string jobId)
        {
            _semaphore.Wait();
            try
            {
                if (!_crawlerDict.ContainsKey(jobId))
                {
                    throw new InvalidOperationException($"No job ({jobId}) found");
                }

                if (_crawlerDict[jobId].All(cs => cs.Id != crawlerId))
                {
                    throw new InvalidOperationException($"No crawler ({crawlerId}) found working on job ({jobId})");
                }

                var crawlerStateIndex = _crawlerDict[jobId].FindIndex(cs => cs.Id == crawlerId);
                _crawlerDict[jobId][crawlerStateIndex] = new CrawlerState(crawlerId, true);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Clear(string jobId)
        {
            _semaphore.Wait();
            try
            {
                if (_crawlerDict.ContainsKey(jobId))
                {
                    _crawlerDict.Remove(jobId);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task WaitAllFinished(string jobId, TimeSpan timeout)
        {
            return Task.Run(async () =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (!_crawlerDict.ContainsKey(jobId))
                    {
                        return Task.CompletedTask;
                    }

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (!_crawlerDict[jobId].All(cs => cs.Finished))
                    {
                        if (stopwatch.Elapsed > timeout)
                        {
                            throw new TimeoutException($"Timed out waiting for all crawlers for job ({jobId}) to finish");
                        }
                        
                        await Task.Delay(50);
                    }

                    return Task.CompletedTask;
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }
    }
}