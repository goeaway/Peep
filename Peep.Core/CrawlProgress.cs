using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core
{
    public class CrawlProgress
    {
        public int CrawlCount { get; set; }
        public TimeSpan Duration { get; set; }
        public int DataCount { get; set; }
    }
}
