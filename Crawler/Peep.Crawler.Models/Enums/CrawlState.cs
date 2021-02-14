using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Models.Enums
{
    public enum CrawlState
    {
        Queued,
        Running,
        Complete,
        Error
    }
}
