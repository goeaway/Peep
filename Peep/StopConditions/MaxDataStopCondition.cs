using Peep.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.StopConditions
{
    public class MaxDataStopCondition : ICrawlStopCondition
    {
        private readonly int _maxDataCount;
        public MaxDataStopCondition(int maxDataCount)
        {
            _maxDataCount = maxDataCount;
        }

        public bool Stop(CrawlProgress progress) => progress.DataCount >= _maxDataCount;
    }
}
