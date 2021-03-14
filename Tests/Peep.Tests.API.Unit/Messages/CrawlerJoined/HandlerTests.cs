using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.CrawlerJoined;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.Core.API.Providers;

namespace Peep.Tests.API.Unit.Messages.CrawlerJoined
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Joined Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Updates_JobCrawler_With_CrawlerId_With_Job()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerJoinedRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID,
            });

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                State = JobState.Running
            });

            await context.SaveChangesAsync();

            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            var handler = new CrawlerJoinedHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            var jobCrawler = context.JobCrawlers.First();
            Assert.AreEqual(JOB_ID, jobCrawler.Job.Id);
            Assert.AreEqual(CRAWLER_ID, jobCrawler.CrawlerId);
            Assert.AreEqual(now, jobCrawler.LastHeartbeat);
            
            Assert.AreEqual(MediatR.Unit.Value, result);
        }

        [TestMethod]
        public async Task Returns_ErrorResponse_If_JobCrawler_Not_Found()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerJoinedRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();

            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            var handler = new CrawlerJoinedHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual($"Crawler with id {CRAWLER_ID} not found", result.Message);
        }

        [TestMethod]
        public async Task Returns_ErrorResponse_If_Job_Not_Found()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerJoinedRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID
            });

            await context.SaveChangesAsync();
            
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            var handler = new CrawlerJoinedHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual($"Job with id {JOB_ID} not found", result.Message);
        }
        
        [TestMethod]
        [DataRow(JobState.Queued)]
        [DataRow(JobState.Cancelled)]
        [DataRow(JobState.Complete)]
        [DataRow(JobState.Errored)]
        public async Task Returns_ErrorResponse_If_Job_Not_In_Crawlable_State(JobState jobState)
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var request = new CrawlerJoinedRequest
            {
                CrawlerId = CRAWLER_ID,
                JobId = JOB_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID
            });

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                State = jobState
            });
            
            await context.SaveChangesAsync();
            
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            var handler = new CrawlerJoinedHandler(context, nowProvider);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual($"Cannot run job in current state ({jobState})", result.Message);
        }
    }
}