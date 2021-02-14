using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Peep.Exceptions
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
