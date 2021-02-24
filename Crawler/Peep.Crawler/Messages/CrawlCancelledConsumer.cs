using MassTransit;
using Paramore.Brighter;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Crawler.Messages
{
    public class CrawlCancelledConsumer : IConsumer<CrawlCancelled>
    {
        private readonly IJobQueue _jobQueue;
        private readonly ICrawlCancellationTokenProvider _cancellationTokenProvider;

        public CrawlCancelledConsumer(
            IJobQueue jobQueue,
            ICrawlCancellationTokenProvider cancellationTokenProvider)
        {
            _jobQueue = jobQueue;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public Task Consume(ConsumeContext<CrawlCancelled> context)
        {
            if (_jobQueue.TryRemove(context.Message.CrawlId))
            {
            }

            if (_cancellationTokenProvider.CancelJob(context.Message.CrawlId))
            {
            }

            return Task.CompletedTask;
        }
    }
}
