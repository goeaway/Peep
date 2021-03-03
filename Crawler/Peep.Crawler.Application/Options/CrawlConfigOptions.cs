namespace Peep.Crawler.Application.Options
{
    public class CrawlConfigOptions
    {
        public const string Key = "Crawl";
        public int ProgressUpdateDataCount { get; set; }
            = 10;
    }
}
