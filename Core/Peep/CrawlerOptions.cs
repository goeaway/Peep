using Peep.Data;
using Peep.Factories;
using System.Net.Http;
using System;
using Peep.Filtering;
using Peep.Queueing;
using Peep.Robots;
using Peep.PageActions;
using Serilog;

namespace Peep
{
    public class CrawlerOptions
    {
        public ILogger Logger { get; set; }
            = new LoggerConfiguration().CreateLogger();
        public IBrowserAdapterFactory BrowserAdapterFactory { get; set; }
            = new PuppeteerSharpBrowserAdapterFactory();
        public IDataExtractor DataExtractor { get; set; }
            = new DataExtractor();
        public IRobotParser RobotParser { get; set; }
            = new RobotParser(new HttpClient());

        public IPageActionPerformer PageActionPerformer { get; set; }
            = new PageActionPerformer();

        public int PageActionRetryCount { get; set; }
            = 3;
        public int QueueEmptyRetryCount { get; set; }
            = 10;
    }
}
