using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.Core.API.Providers;
using Peep.StopConditions;
using Peep.Tests.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Commands.QueueCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Queue Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Returns_Response_With_Id()
        {
            var job = new StoppableCrawlJob();
            var request = new QueueCrawlRequest(job);

            using var context = Setup.CreateContext();
            var nowProvider = new NowProvider();

            var handler = new QueueCrawlHandler(context, nowProvider);

            var response = await handler.Handle(request, CancellationToken.None);

            var saved = context.QueuedJobs.Find(response.CrawlId);
            Assert.AreEqual(response.CrawlId, saved.Id);
        }

        [TestMethod]
        public async Task Adds_Entry_To_Db_With_Job_With_Id_And_Queue_Date()
        {
            var job = new StoppableCrawlJob();
            var request = new QueueCrawlRequest(job);

            using var context = Setup.CreateContext();
            var testNow = new DateTime(2021, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var handler = new QueueCrawlHandler(context, nowProvider);

            var response = await handler.Handle(request, CancellationToken.None);

            var saved = context.QueuedJobs.Find(response.CrawlId);

            Assert.IsNotNull(saved.JobJson);
            Assert.IsNotNull(saved.Id);
            Assert.AreEqual(testNow, saved.DateQueued);
        }

        [TestMethod]
        public async Task Adds_Max_Crawl_Stop_Condition()
        {
            var job = new StoppableCrawlJob();
            var request = new QueueCrawlRequest(job);

            using var context = Setup.CreateContext();
            var testNow = new DateTime(2021, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var handler = new QueueCrawlHandler(context, nowProvider);

            var response = await handler.Handle(request, CancellationToken.None);

            var saved = context.QueuedJobs.Find(response.CrawlId);

            var savedJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(saved.JobJson);

            var savedStopCondition = 
                savedJob
                    .StopConditions
                    .First(sc => (sc as SerialisableStopCondition).Type == SerialisableStopConditionType.MaxCrawlCount)
                    as SerialisableStopCondition;
            Assert.AreEqual((long)1_000_000, savedStopCondition.Value);
        }

        [TestMethod]
        public async Task Adds_Max_Duration_Stop_Condition()
        {
            var job = new StoppableCrawlJob();
            var request = new QueueCrawlRequest(job);

            using var context = Setup.CreateContext();
            var testNow = new DateTime(2021, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var handler = new QueueCrawlHandler(context, nowProvider);

            var response = await handler.Handle(request, CancellationToken.None);

            var saved = context.QueuedJobs.Find(response.CrawlId);

            var savedJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(saved.JobJson);

            var savedStopCondition =
                savedJob
                    .StopConditions
                    .First(sc => (sc as SerialisableStopCondition).Type == SerialisableStopConditionType.MaxDurationSeconds)
                    as SerialisableStopCondition;
            Assert.AreEqual(TimeSpan.FromDays(1).TotalSeconds, savedStopCondition.Value);
        }

        [TestMethod]
        public async Task Adds_Default_Stop_Condition_If_StopConditions_Null()
        {
            var job = new StoppableCrawlJob { StopConditions = null };

            var request = new QueueCrawlRequest(job);

            using var context = Setup.CreateContext();
            var testNow = new DateTime(2021, 01, 01);
            var nowProvider = new NowProvider(testNow);

            var handler = new QueueCrawlHandler(context, nowProvider);

            var response = await handler.Handle(request, CancellationToken.None);

            var saved = context.QueuedJobs.Find(response.CrawlId);

            var savedJob = JsonConvert.DeserializeObject<StoppableCrawlJob>(saved.JobJson);

            Assert.AreEqual(2, savedJob.StopConditions.Count());
        }
    }
}
