using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class CrawlOptions
    {
        /// <summary>
        /// Gets or sets a regex pattern string used to determine if a found url should be queued for crawling. 
        /// If a url matches this regex it should be added.
        /// </summary>
        public string UriRegex { get; set; }
        /// <summary>
        /// Gets or sets a regex pattern string used to find and extract HTML content from a web page.
        /// If a part of a page's HTML content matches this regex it should be saved
        /// </summary>
        public string DataRegex { get; set; }
        /// <summary>
        /// Gets or sets a flag indicating the crawler should ignore a domain's robots.txt directives.
        /// </summary>
        public bool IgnoreRobots { get; set; }
        /// <summary>
        /// Gets or sets options for waiting during a crawl
        /// </summary>
        public WaitOptions WaitOptions { get; set; }
        /// <summary>
        /// Gets or sets a collection of stop conditions used by the crawler to determine when a crawl should be stopped.
        /// The default stop conditions are a max crawl count of 10,000 and max crawl time of 20 minutes. 
        /// If any stop condition is met the crawl will stop
        /// </summary>
        public IEnumerable<ICrawlStopCondition> StopConditions { get; set; }
            = new List<ICrawlStopCondition>
            {
                new MaxCrawlStopCondition(10000),
                new MaxDurationStopCondition(TimeSpan.FromMinutes(20))
            };
    }
}
