using Peep.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.StopConditions
{
    public class MaxCrawlStopCondition : ICrawlStopCondition
    {
        private readonly int _max;

        public MaxCrawlStopCondition(int max)
        {
            _max = max;
        }

        public bool Stop(CrawlProgress progress) => progress.CrawlCount >= _max;
    }
}
