using System;

namespace Peep.Core.Exceptions
{
    public class CrawlerOptionsException : Exception
    {
        public CrawlerOptionsException()
        {
        }

        public CrawlerOptionsException(string message) : base(message)
        {
        }
    }
}
