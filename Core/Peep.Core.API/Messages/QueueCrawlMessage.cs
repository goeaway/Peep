using Paramore.Brighter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.API.Messages
{
    public class QueueCrawlMessage : IRequest
    {
        public Guid Id { get; set; }
            = Guid.NewGuid();

        public IdentifiableCrawlJob Job { get; set; }

        public QueueCrawlMessage(IdentifiableCrawlJob job)
        {
            Job = job;
        }
    }
}
