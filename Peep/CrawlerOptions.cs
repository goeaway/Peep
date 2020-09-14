using Peep.Abstractions;
using Peep.Data;
using Peep.Factories;
using System.Net.Http;
using System;

namespace Peep
{
    public class CrawlerOptions
    {
        public uint Threads { get; set; }
            = 1;
        public IBrowserFactory BrowserFactory { get; set; }
            = new BrowserFactory();
        public IDataExtractor DataExtractor { get; set; }
            = new DataExtractor();
        public IRobotParser RobotParser { get; set; }
            = new RobotParser(new HttpClient());
    }
}
