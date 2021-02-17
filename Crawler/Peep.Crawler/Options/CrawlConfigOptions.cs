using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Options
{
    public class CrawlConfigOptions
    {
        public const string Key = "Crawl";
        public int ProgressUpdateMilliseconds { get; set; }
            = 1000;
    }
}
