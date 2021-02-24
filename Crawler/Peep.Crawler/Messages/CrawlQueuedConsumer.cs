using MassTransit;
using Peep.Core.Infrastructure.Messages;
using Peep.Core.Infrastructure.Queuing;
using Peep.Crawler;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Crawler.Messages
{
    public class CrawlQueuedConsumer : IConsumer<CrawlQueued>
    {
        public readonly IJobQueue _jobQueue;

        public CrawlQueuedConsumer(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public Task Consume(ConsumeContext<CrawlQueued> context)
        {
            _jobQueue.Enqueue(context.Message.Job);

            return Task.CompletedTask;
        }
    }
}
