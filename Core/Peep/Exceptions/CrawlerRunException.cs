using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Exceptions
{
    public class CrawlerRunException : PeepException
    {
        public CrawlProgress CrawlProgress { get; set; }

        public CrawlerRunException()
        {
        }

        public CrawlerRunException(string message) : base(message)
        {
        }

        public CrawlerRunException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CrawlerRunException(string message, CrawlProgress crawlProgress) : base(message)
        {
            CrawlProgress = crawlProgress;
        }

        public CrawlerRunException(string message, CrawlProgress crawlProgress, Exception innerException) : base (message, innerException)
        {
            CrawlProgress = crawlProgress;
        }
    }
}
