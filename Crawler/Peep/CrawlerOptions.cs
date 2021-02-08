using Peep.Data;
using Peep.Factories;
using System.Net.Http;
using System;
using Peep.Filtering;

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
        public IFilter Filter { get; set; }
            = new BloomFilter(1_000_000);
    }
}
