using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.StopConditions
{
    public interface ICrawlStopCondition
    {
        bool Stop(CrawlProgress progress);
    }
}
