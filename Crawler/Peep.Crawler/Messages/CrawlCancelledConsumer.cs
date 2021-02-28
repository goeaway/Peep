using MassTransit;
using Paramore.Brighter;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Peep.Crawler.Messages
{
    public class CrawlCancelledConsumer : IConsumer<CrawlCancelled>
    {
        private readonly ILogger _logger;
        private readonly IJobQueue _jobQueue;
        private readonly ICrawlCancellationTokenProvider _cancellationTokenProvider;

        public CrawlCancelledConsumer(
            ILogger logger,
            IJobQueue jobQueue,
            ICrawlCancellationTokenProvider cancellationTokenProvider)
        {
            _logger = logger;
            _jobQueue = jobQueue;
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        public Task Consume(ConsumeContext<CrawlCancelled> context)
        {
            if (_jobQueue.TryRemove(context.Message.CrawlId))
            {
                _logger.Information("Removed job {Id} from queue", context.Message.CrawlId);
                return Task.CompletedTask;
            }

            if (_cancellationTokenProvider.CancelJob(context.Message.CrawlId))
            {
                _logger.Information("Cancelled running job {Id}", context.Message.CrawlId);
                return Task.CompletedTask;
            }

            _logger.Warning("Could not find job {Id} to cancel", context.Message.CrawlId);
            return Task.CompletedTask;
        }
    }
}
