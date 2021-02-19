using Paramore.Brighter;
using Peep.Core.API.Messages;
using Peep.Core.API.Providers;
using Peep.Crawler;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Subscriptions
{
    public class CancelCrawlHandler : RequestHandler<CancelCrawlMessage>
    {
        private readonly IJobQueue _jobQueue;
        private readonly ICrawlCancellationTokenProvider _cancellationTokenProvider;

        public CancelCrawlHandler(
            IJobQueue jobQueue,
            ICrawlCancellationTokenProvider cancellationTokenProvider)
        {
            _jobQueue = jobQueue;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public override CancelCrawlMessage Handle(CancelCrawlMessage command)
        {
            if(_jobQueue.TryRemove(command.CrawlId))
            {
            }

            if(_cancellationTokenProvider.CancelJob(command.CrawlId))
            {
            }

            return base.Handle(command);
        }
    }
}
