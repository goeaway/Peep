using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Commands.MonitorCrawlers;
using Peep.API.Models.Entities;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;
using Serilog;

namespace Peep.Tests.API.Unit.Commands.MonitorCrawlers
{
    [TestClass]
    [TestCategory("API - Unit - Monitor Crawlers Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Removes_All_JobCrawlers_Whose_LastHeartbeat_Was_Over_Three_Ticks_Ago()
        {
            const string CRAWLER_1 = "crawler1", CRAWLER_2 = "crawler2", JOB_ID = "id";
            
            var request = new MonitorCrawlersRequest();

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            await context.JobCrawlers.AddRangeAsync(new List<JobCrawler>
            {
                new JobCrawler
                {
                    CrawlerId = CRAWLER_1,
                    LastHeartbeat = now.AddMilliseconds(-3001)
                },
                new JobCrawler
                {
                    CrawlerId = CRAWLER_2,
                    Job = new Job
                    {
                        Id = JOB_ID
                    },
                    LastHeartbeat = now.AddMilliseconds(-3001)
                }
            });

            await context.SaveChangesAsync();

            var monitoringOptions = new MonitoringOptions
            {
                TickSeconds = 1,
                MaxUnresponsiveTicks = 3
            };
            
            var handler = new MonitorCrawlersHandler(context, nowProvider, monitoringOptions, new LoggerConfiguration().CreateLogger());

            await handler.Handle(request, CancellationToken.None);
            
            Assert.IsFalse(context.JobCrawlers.Any());
        }
        
        [TestMethod]
        public async Task Does_Not_Removes_JobCrawlers_Whose_LastHeartbeat_Was_Less_Than_Three_Ticks_Ago()
        {
            const string CRAWLER_1 = "crawler1";
            
            var request = new MonitorCrawlersRequest();

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            await context.JobCrawlers.AddRangeAsync(new List<JobCrawler>
            {
                new JobCrawler
                {
                    CrawlerId = CRAWLER_1,
                    LastHeartbeat = now.AddMilliseconds(-3000)
                }
            });

            await context.SaveChangesAsync();

            var monitoringOptions = new MonitoringOptions
            {
                TickSeconds = 1,
                MaxUnresponsiveTicks = 3
            };
            
            var handler = new MonitorCrawlersHandler(context, nowProvider, monitoringOptions, new LoggerConfiguration().CreateLogger());

            await handler.Handle(request, CancellationToken.None);
            
            Assert.IsTrue(context.JobCrawlers.Any());
        }
    }
}