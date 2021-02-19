using Paramore.Brighter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.API.Messages
{
    public class CancelCrawlMessage : IRequest
    {
        public Guid Id { get; set; }
            = Guid.NewGuid();

        public string CrawlId { get; set; }

        public CancelCrawlMessage(string crawlId)
        {
            CrawlId = crawlId;
        }
    }
}
