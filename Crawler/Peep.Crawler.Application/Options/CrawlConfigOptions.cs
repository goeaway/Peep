using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Application.Options
{
    public class CrawlConfigOptions
    {
        public const string Key = "Crawl";
        public int ProgressUpdateMilliseconds { get; set; }
            = 1000;
    }
}
