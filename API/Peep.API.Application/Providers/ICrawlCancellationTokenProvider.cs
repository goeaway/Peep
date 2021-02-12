using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Peep.API.Application.Providers
{
    public interface ICrawlCancellationTokenProvider
    {
        bool CancelJob(string jobId);
        CancellationToken GetToken(string jobId);
        bool DisposeOfToken(string jobId);
    }
}
