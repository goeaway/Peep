using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peep.Filtering;
using Peep.Queueing;

namespace Peep.Crawler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogger();
                    services.AddCrawler();
                    services.AddMessagingOptions(hostContext.Configuration);
                    services.AddCrawlerOptions(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                    services.AddTransient<ICrawlFilter>(provider => new BloomFilter(1_000_000));
                    services.AddTransient<ICrawlQueue>(provider => new CrawlQueue());
                });
    }
}
