using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Providers;
using Peep.API.Application.Queries.GetCrawl;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Models.Mappings;
using Peep.Core.API.Exceptions;
using Peep.Tests.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Queries.GetCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Get Crawl Handler")]
    public class HandlerTests
    {
        private static IMapper CreateMapper() => new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<QueuedJobProfile>();
            cfg.AddProfile<RunningJobProfile>();
            cfg.AddProfile<CompletedJobProfile>();
        }).CreateMapper();

        [TestMethod]
        public async Task Returns_Complete_Crawl_Result_If_Crawl_Is_Complete()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DATE_STARTED = new DateTime(2022, 01, 01);
            var DATE_COMPLETED = new DateTime(2023, 01, 01);
            var DATA_FIRST_KEY = "http://localhost/";
            var DATA_FIRST_VALUE = "data";
            var DATA_JSON = JsonConvert.SerializeObject(new Dictionary<Uri, IEnumerable<string>>()
            {
                { new Uri(DATA_FIRST_KEY), new List<string> { DATA_FIRST_VALUE } }
            });
            var DURATION = TimeSpan.FromSeconds(1);

            var request = new GetCrawlRequest(ID);

            using var context = Setup.CreateContext();

            context.CompletedJobs.Add(new CompletedJob
            {
                Id = ID,
                DateQueued = DATE_QUEUED,
                DateCompleted = DATE_COMPLETED,
                DateStarted = DATE_STARTED,
                CrawlCount = CRAWL_COUNT,
                DataJson = DATA_JSON,
                Duration = DURATION
            });

            context.SaveChanges();

            var handler = new GetCrawlHandler(context, CreateMapper());

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(DURATION, result.Duration);

            Assert.AreEqual(DATE_QUEUED, result.DateQueued);
            Assert.AreEqual(DATE_COMPLETED, result.DateCompleted);
            Assert.AreEqual(DATE_STARTED, result.DateStarted);

            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(CrawlState.Complete, result.State);
        }

        [TestMethod]
        public async Task Returns_Empty_Crawl_Result_With_Queued_Status_If_Crawl_Is_Queued()
        {
            const string ID = "random id";
            var DATE_QUEUED = new DateTime(2021, 01, 01);

            var request = new GetCrawlRequest(ID);

            using var context = Setup.CreateContext();

            context.QueuedJobs.Add(new QueuedJob
            {
                Id = ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob()),
                DateQueued = DATE_QUEUED
            });

            context.SaveChanges();

            var handler = new GetCrawlHandler(context, CreateMapper());

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(ID, result.Id);
            
            Assert.IsFalse(result.Data.Any());
            Assert.AreEqual(0, result.CrawlCount);
            Assert.AreEqual(default, result.Duration);
            
            Assert.AreEqual(DATE_QUEUED, result.DateQueued);
            Assert.IsNull(result.DateCompleted);
            Assert.IsNull(result.DateStarted);
            
            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(CrawlState.Queued, result.State);
        }

        [TestMethod]
        public async Task Returns_Incomplete_Crawl_Result_With_Running_Status_If_Crawl_Is_Running()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 1;
            var DATA_FIRST_KEY = "http://localhost/";
            var DATA_FIRST_VALUE = "data";
            var DATA_JSON = JsonConvert.SerializeObject(new Dictionary<Uri, IEnumerable<string>>()
            {
                { new Uri(DATA_FIRST_KEY), new List<string> { DATA_FIRST_VALUE } }
            });
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DATE_STARTED = new DateTime(2022, 01, 01);
            var DURATION = TimeSpan.FromSeconds(1);

            var request = new GetCrawlRequest(ID);

            using var context = Setup.CreateContext();

            context.RunningJobs.Add(new RunningJob
            {
                Id = ID,
                DataJson = DATA_JSON,
                DateQueued = DATE_QUEUED,
                DateStarted = DATE_STARTED,
                CrawlCount = CRAWL_COUNT,
                Duration = DURATION
            });

            context.SaveChanges();

            var handler = new GetCrawlHandler(context, CreateMapper());

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(DURATION, result.Duration);

            Assert.AreEqual(DATE_QUEUED, result.DateQueued);
            Assert.AreEqual(DATE_STARTED, result.DateStarted);
            Assert.IsNull(result.DateCompleted);

            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(CrawlState.Running, result.State);
        }

        [TestMethod]
        public async Task Returns_Complete_Crawl_Error_Result_With_Error_Status_If_Crawl_Ran_But_Errored()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            var DATE_QUEUED = new DateTime(2021, 01, 01);
            var DATE_STARTED = new DateTime(2022, 01, 01);
            var DATE_COMPLETED = new DateTime(2023, 01, 01);
            var DATA_FIRST_KEY = "http://localhost/";
            var DATA_FIRST_VALUE = "data";
            var DATA_JSON = JsonConvert.SerializeObject(new Dictionary<Uri, IEnumerable<string>>() 
            {
                { new Uri(DATA_FIRST_KEY), new List<string> { DATA_FIRST_VALUE } }
            });
            var DURATION = TimeSpan.FromSeconds(1);
            var ERRORS = "somthing bad,something else bad";

            var request = new GetCrawlRequest(ID);

            using var context = Setup.CreateContext();

            context.CompletedJobs.Add(new CompletedJob
            {
                Duration = DURATION,
                DataJson = DATA_JSON,
                Errors = ERRORS,
                DateStarted = DATE_STARTED,
                DateCompleted = DATE_COMPLETED,
                DateQueued = DATE_QUEUED,
                CrawlCount = CRAWL_COUNT,
                Id = ID
            });

            context.SaveChanges();

            var handler = new GetCrawlHandler(context, CreateMapper());

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(DURATION, result.Duration);

            Assert.AreEqual(DATE_QUEUED, result.DateQueued);
            Assert.AreEqual(DATE_COMPLETED, result.DateCompleted);
            Assert.AreEqual(DATE_STARTED, result.DateStarted);

            Assert.AreEqual(ERRORS, result.ErrorMessage);
            Assert.AreEqual(CrawlState.Error, result.State);
        }

        [TestMethod]
        public async Task Throws_Error_If_Crawl_Id_Does_Not_Match_A_Queued_Or_Completed_Crawl()
        {
            const string ID = "random id";

            var request = new GetCrawlRequest(ID);

            using var context = Setup.CreateContext();

            var handler = new GetCrawlHandler(context, CreateMapper());

            await Assert.ThrowsExceptionAsync<RequestFailedException>(
                () => handler.Handle(request, CancellationToken.None));
        }
    }
}
