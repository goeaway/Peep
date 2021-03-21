namespace Peep.Core
{
    public class IdentifiableCrawlJob : CrawlJob
    {
        public string Id { get; set; }

        public IdentifiableCrawlJob() { }

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
