using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Peep.Crawler.Application.Providers
{
    public class CrawlCancellationTokenProvider : ICrawlCancellationTokenProvider
    {
        private readonly Dictionary<string, CancellationTokenSource> _tokenDictionary
            = new Dictionary<string, CancellationTokenSource>();

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
            _tokenDictionary.Add(jobId, newSource);
            return newSource.Token;
        }

        public bool DisposeOfToken(string jobId)
        {
            return _tokenDictionary.Remove(jobId);
        }
    }
}
