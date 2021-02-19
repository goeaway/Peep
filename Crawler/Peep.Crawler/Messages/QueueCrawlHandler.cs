using Paramore.Brighter;
using Peep.Core.API.Messages;
using Peep.Crawler;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.Infrastructure.Subscriptions
{
    public class QueueCrawlHandler : RequestHandler<QueueCrawlMessage>
    {
        public readonly IJobQueue _jobQueue;

        public QueueCrawlHandler(IJobQueue jobQueue)
        {
            _jobQueue = jobQueue;
        }

        public override QueueCrawlMessage Handle(QueueCrawlMessage command)
        {
            _jobQueue.Enqueue(command.Job);

            return base.Handle(command);
        }
    }
}
