using Peep.Data;
using Peep.Factories;
using System.Net.Http;
using System;
using Peep.Filtering;
using Peep.Queueing;
using Peep.Robots;

namespace Peep
{
    public class CrawlerOptions
    {
        public IBrowserAdapterFactory BrowserAdapterFactory { get; set; }
            = new PuppeteerSharpBrowserAdapterFactory();
        public IDataExtractor DataExtractor { get; set; }
            = new DataExtractor();
        public IRobotParser RobotParser { get; set; }
            = new RobotParser(new HttpClient());
        public int PageActionRetryCount { get; set; }
            = 3;
        public int QueueEmptyRetryCount { get; set; }
            = 10;
    }
}
