using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.CrawlerHeartbeat;
using Peep.API.Models.Entities;
using Peep.Core.API.Providers;

namespace Peep.Tests.API.Unit.Messages.CrawlerHeartbeat
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Heartbeat Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Updates_LastHeartbeat_For_Crawler()
        {
            const string CRAWLER_ID = "crawler";

            var request = new CrawlerHeartbeatRequest
            {
                CrawlerId = CRAWLER_ID
            };
            
            await using var context = Setup.CreateContext();
            
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID,
                LastHeartbeat = now.AddSeconds(-1)
            });

            await context.SaveChangesAsync();
            
            var handler = new CrawlerHeartbeatHandler(context, nowProvider);

            await handler.Handle(request, CancellationToken.None);

            var jobCrawler = context.JobCrawlers.First();
            
            Assert.AreEqual(now, jobCrawler.LastHeartbeat);
        }
        
        [TestMethod]
        public async Task Returns_MessageErrorResponse_If_Crawler_Not_Found()
        {
            const string CRAWLER_ID = "crawler";

            var request = new CrawlerHeartbeatRequest
            {
                CrawlerId = CRAWLER_ID
            };
            
            await using var context = Setup.CreateContext();
            
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            var handler = new CrawlerHeartbeatHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;

            Assert.AreEqual($"Crawler with id {CRAWLER_ID} not found", result.Message);
        }
    }
}