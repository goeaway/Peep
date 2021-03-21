using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Peep.Core.PageActions;

namespace Peep.Core
{
    public class CrawlJob
    {
        public CrawlJob()
        {

        }

        [JsonConstructor]
        public CrawlJob(
            IEnumerable<SerialisablePageAction> pageActions)
        {
            PageActions = pageActions;
        }

        /// <summary>
        /// Gets or sets a collection of seed uris a crawler should begin a crawl from
        /// </summary>
        public IEnumerable<Uri> Seeds { get; set; }
            = new List<Uri>();
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
        /// Gets or sets a collection of actions to be performed on each page
        /// </summary>
        public IEnumerable<IPageAction> PageActions { get; set; }
            = new List<IPageAction>();
    }
}
