using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.API.Models.Mappings;
using Peep.Core.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Peep.Data;

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
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var dateCompleted = new DateTime(2023, 01, 01);
            var duration = TimeSpan.FromSeconds(1);

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.CompletedJobs.AddAsync(new CompletedJob
            {
                Id = ID,
                DateQueued = dateQueued,
                DateCompleted = dateCompleted,
                DateStarted = dateStarted,
                CrawlCount = CRAWL_COUNT,
                CompletedJobData = new List<CompletedJobData>
                {
                    new CompletedJobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
                Duration = duration
            });

            await context.SaveChangesAsync();

            var mockDataManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();

            var handler = new GetCrawlHandler(context, mockDataManager.Object, CreateMapper());

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(duration, result.Duration);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateCompleted, result.DateCompleted);
            Assert.AreEqual(dateStarted, result.DateStarted);

            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(CrawlState.Complete, result.State);
        }

        [TestMethod]
        public async Task Returns_Empty_Crawl_Result_With_Queued_Status_If_Crawl_Is_Queued()
        {
            const string ID = "random id";
            var dateQueued = new DateTime(2021, 01, 01);

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.QueuedJobs.AddAsync(new QueuedJob
            {
                Id = ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob()),
                DateQueued = dateQueued
            });

            await context.SaveChangesAsync();

            var mockDataManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();

            var handler = new GetCrawlHandler(context, mockDataManager.Object, CreateMapper());

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);
            
            Assert.IsFalse(result.Data.Any());
            Assert.AreEqual(0, result.CrawlCount);
            Assert.AreEqual(default, result.Duration);
            
            Assert.AreEqual(dateQueued, result.DateQueued);
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
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var duration = TimeSpan.FromSeconds(1);

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.RunningJobs.AddAsync(new RunningJob
            {
                Id = ID,
                DateQueued = dateQueued,
                DateStarted = dateStarted,
                CrawlCount = CRAWL_COUNT,
                Duration = duration
            });

            await context.SaveChangesAsync();

            var mockDataManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();

            mockDataManager
                .Setup(mock => mock.GetData(ID))
                .ReturnsAsync(new ExtractedData
                {
                    {new Uri(DATA_FIRST_KEY), new List<string> {DATA_FIRST_VALUE}}
                });

            var handler = new GetCrawlHandler(context, mockDataManager.Object, CreateMapper());

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(duration, result.Duration);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateStarted, result.DateStarted);
            Assert.IsNull(result.DateCompleted);

            Assert.IsNull(result.ErrorMessage);
            Assert.AreEqual(CrawlState.Running, result.State);
        }

        [TestMethod]
        public async Task Returns_Complete_Crawl_Error_Result_With_Error_Status_If_Crawl_Ran_But_Errored()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            const string ERRORS = "something bad,something else bad";
            
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var dateCompleted = new DateTime(2023, 01, 01);
            var duration = TimeSpan.FromSeconds(1);

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.CompletedJobs.AddAsync(new CompletedJob
            {
                Duration = duration,
                CompletedJobData = new List<CompletedJobData>
                {
                    new CompletedJobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
                DateStarted = dateStarted,
                DateCompleted = dateCompleted,
                DateQueued = dateQueued,
                CrawlCount = CRAWL_COUNT,
                ErrorMessage = ERRORS,
                Id = ID
            });

            await context.SaveChangesAsync();

            var mockDataManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();

            var handler = new GetCrawlHandler(context, mockDataManager.Object, CreateMapper());

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(duration, result.Duration);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateCompleted, result.DateCompleted);
            Assert.AreEqual(dateStarted, result.DateStarted);

            Assert.AreEqual(ERRORS, result.ErrorMessage);
            Assert.AreEqual(CrawlState.Complete, result.State);
        }

        [TestMethod]
        public async Task Returns_Error_If_Crawl_Id_Does_Not_Match_A_Queued_Or_Completed_Crawl()
        {
            const string ID = "random id";

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            var mockDataManager = new Mock<ICrawlDataSinkManager<ExtractedData>>();

            var handler = new GetCrawlHandler(context, mockDataManager.Object, CreateMapper());

            var result = await handler.Handle(request, CancellationToken.None);

            var error = result.ErrorOrDefault;
            
            Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
            Assert.AreEqual("Crawl not found", error.Message);
        }
    }
}
