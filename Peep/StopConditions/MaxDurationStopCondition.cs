using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.StopConditions
{
    public class MaxDurationStopCondition : ICrawlStopCondition
    {
        private readonly TimeSpan _max;

        public MaxDurationStopCondition(TimeSpan max)
        {
            _max = max;
        }

        public bool Stop(CrawlProgress progress) => progress.Duration >= _max;
    }
}
