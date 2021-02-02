using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core
{
    public interface ICrawlStopCondition
    {
        bool Stop(CrawlProgress progress);
    }
}
