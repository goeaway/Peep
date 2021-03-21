using Peep.Data;
using System.Net.Http;
using Peep.Core.PageActions;
using Peep.Robots;
using Serilog;

namespace Peep
{
    public class CrawlerOptions
    {
        public ILogger Logger { get; set; }
            = new LoggerConfiguration().CreateLogger();
        public IDataExtractor DataExtractor { get; set; }
            = new DataExtractor();
        public IRobotParser RobotParser { get; set; }
            = new RobotParser(new HttpClient());
        public IPageActionPerformer PageActionPerformer { get; set; }
            = new PageActionPerformer();
        public int PageActionRetryCount { get; set; }
            = 3;
    }
}
