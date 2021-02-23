using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class IdentifiableCrawlJob : CrawlJob
    {
        public string Id { get; set; }

        public IdentifiableCrawlJob(CrawlJob job, string id)
        {
            Id = id;
            DataRegex = job.DataRegex;
            IgnoreRobots = job.IgnoreRobots;
            PageActions = job.PageActions;
            Seeds = job.Seeds;
            UriRegex = job.UriRegex;
        }
    }
}
