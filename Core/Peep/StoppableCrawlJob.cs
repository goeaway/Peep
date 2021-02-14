using Newtonsoft.Json;
using Peep.PageActions;
using Peep.StopConditions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class StoppableCrawlJob : CrawlJob
    {
        public StoppableCrawlJob()
        {

        }

        [JsonConstructor]
        public StoppableCrawlJob(
            IEnumerable<SerialisableStopCondition> stopConditions, 
            IEnumerable<SerialisablePageAction> pageActions) : base (pageActions)
        {
            StopConditions = stopConditions;
        }

        /// <summary>
        /// Gets or sets a collection of stop conditions used by the crawler to determine when a crawl should be stopped.
        /// The default stop conditions are a max crawl count of 10,000 and max crawl time of 20 minutes. 
        /// If any stop condition is met the crawl will stop
        /// </summary>
        public IEnumerable<ICrawlStopCondition> StopConditions { get; set; }
            = new List<ICrawlStopCondition>();
    }
}
