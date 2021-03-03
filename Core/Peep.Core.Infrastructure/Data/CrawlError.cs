using System;
using System.Dynamic;

namespace Peep.Core.Infrastructure.Data
{
    public class CrawlError
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}