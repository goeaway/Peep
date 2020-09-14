using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class CrawlResult
    {
        public int CrawlCount { get; set; }
        public TimeSpan Duration { get; set; }
        public IDictionary<Uri, IEnumerable<string>> Data { get; set; }
    }
}
