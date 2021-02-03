using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.StopConditions
{
    public interface ICrawlStopCondition
    {
        bool Stop(CrawlProgress progress);
    }
}
