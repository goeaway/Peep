using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Peep.Exceptions
{
    public class CrawlerEmptyQueueRetryLimitExceededException : Exception
    {
        public CrawlerEmptyQueueRetryLimitExceededException()
        {
        }

        public CrawlerEmptyQueueRetryLimitExceededException(string message) : base(message)
        {
        }

        public CrawlerEmptyQueueRetryLimitExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
