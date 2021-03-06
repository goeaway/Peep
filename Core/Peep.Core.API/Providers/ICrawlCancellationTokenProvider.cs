﻿using System.Threading;

namespace Peep.Core.API.Providers
{
    public interface ICrawlCancellationTokenProvider
    {
        bool CancelJob(string jobId);
        CancellationToken GetToken(string jobId);
        bool DisposeOfToken(string jobId);
    }
}
