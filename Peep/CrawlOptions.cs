using Peep.Abstractions;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class CrawlOptions
    {
        public string DataRegex { get; set; }
        public bool IgnoreRobots { get; set; }
        public IEnumerable<ICrawlStopCondition> StopConditions { get; set; }
            = new List<ICrawlStopCondition>
            {
                new MaxCrawlStopCondition(10000),
                new MaxDurationStopCondition(TimeSpan.FromMinutes(20))
            };
    }
}
