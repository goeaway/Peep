using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Options
{
    public class CrawlConfigOptions
    {
        public const string Key = "Crawl";
        public int ProgressUpdateDataCount { get; set; }
            = 10;
    }
}
