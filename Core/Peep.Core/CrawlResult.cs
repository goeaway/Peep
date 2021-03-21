using System;

namespace Peep.Core
{
    public class CrawlResult
    {
        public int CrawlCount { get; set; }
        public TimeSpan Duration { get; set; }
        public int DataCount { get; set; }
    }
}
