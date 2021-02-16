using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Peep.Crawler.Application.Providers
{
    public class CrawlCancellationTokenProvider : ICrawlCancellationTokenProvider
    {
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _tokenDictionary
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        public bool CancelJob(string jobId)
        {
            if(_tokenDictionary.TryGetValue(jobId, out var tokenSource))
            {
                tokenSource.Cancel();
                return true;
            }

            return false;
        }

        public CancellationToken GetToken(string jobId)
        {
            if(_tokenDictionary.TryGetValue(jobId, out var tokenSource))
            {
                return tokenSource.Token;
            }

            var newSource = new CancellationTokenSource();
            if(_tokenDictionary.TryAdd(jobId, newSource))
            {
                return newSource.Token;
            }

            // if we got here, call again, one of the above statements WILL be true at some point...
            return GetToken(jobId);
        }

        public bool DisposeOfToken(string jobId)
        {
            return _tokenDictionary.TryRemove(jobId, out var _);
        }
    }
}
