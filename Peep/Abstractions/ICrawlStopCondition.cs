using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Abstractions
{
    public interface ICrawlStopCondition
    {
        bool Stop(CrawlProgress progress);
    }
}
