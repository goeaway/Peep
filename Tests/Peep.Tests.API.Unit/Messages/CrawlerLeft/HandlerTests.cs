using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.CrawlerLeft;
using Peep.API.Models.Entities;
using Peep.Core.API.Providers;

namespace Peep.Tests.API.Unit.Messages.CrawlerLeft
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Left Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Removes_Job_From_JobCrawler()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerLeftRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID,
                Job = new Job
                {
                    Id = JOB_ID
                }
            });

            await context.SaveChangesAsync();
            
            var handler = new CrawlerLeftHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            var jobCrawler = context.JobCrawlers.First();
            
            Assert.AreEqual(CRAWLER_ID, jobCrawler.CrawlerId);
            Assert.IsNull(jobCrawler.Job);
            Assert.AreEqual(1, context.Jobs.Count());
            Assert.AreEqual(now, jobCrawler.LastHeartbeat);
            Assert.AreEqual(MediatR.Unit.Value, result);
        }
        
        [TestMethod]
        public async Task Returns_ErrorResponse_If_JobCrawler_Not_Found()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerLeftRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            var handler = new CrawlerLeftHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;

            Assert.AreEqual($"Crawler with id {CRAWLER_ID} not found", result.Message);
        }
    }
}