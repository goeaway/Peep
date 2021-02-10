using Peep.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Exceptions
{
    public class CrawlerRunException : PeepException
    {
        public CrawlResult CrawlResult { get; set; }

        public CrawlerRunException()
        {
        }

        public CrawlerRunException(string message) : base(message)
        {
        }

        public CrawlerRunException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public CrawlerRunException(string message, CrawlResult crawlResult) : base(message)
        {
            CrawlResult = crawlResult;
        }

        public CrawlerRunException(string message, CrawlResult crawlResult, Exception innerException) : base (message, innerException)
        {
            CrawlResult = crawlResult;
        }
    }
}
