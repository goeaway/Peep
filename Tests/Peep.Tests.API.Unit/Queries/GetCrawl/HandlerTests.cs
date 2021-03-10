using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Models.Entities;
using Peep.API.Models.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Peep.API.Models.Enums;
using Peep.Core.API.Providers;

namespace Peep.Tests.API.Unit.Queries.GetCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Get Crawl Handler")]
    public class HandlerTests
    {
        private static IMapper CreateMapper(INowProvider nowProvider) => new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new JobProfile(nowProvider));
        }).CreateMapper();

        [TestMethod]
        public async Task Returns_Complete_Crawl_Result_If_Crawl_Is_Complete()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            const int DATA_COUNT = 1;
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var dateCompleted = new DateTime(2023, 01, 01);
            var nowProvider = new NowProvider();
            
            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = ID,
                DateQueued = dateQueued,
                DateCompleted = dateCompleted,
                DateStarted = dateStarted,
                CrawlCount = CRAWL_COUNT,
                State = JobState.Complete,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
            });

            await context.SaveChangesAsync();

            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(DATA_COUNT, result.DataCount);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateCompleted, result.DateCompleted);
            Assert.AreEqual(dateStarted, result.DateStarted);
            Assert.AreEqual(dateCompleted - dateStarted, result.Duration);

            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(JobState.Complete, result.State);
        }
        
        [TestMethod]
        public async Task Returns_Complete_Crawl_Result_If_Crawl_Is_Cancelled()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            const int DATA_COUNT = 1;
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var dateCompleted = new DateTime(2023, 01, 01);
            var nowProvider = new NowProvider();
            
            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = ID,
                DateQueued = dateQueued,
                DateCompleted = dateCompleted,
                DateStarted = dateStarted,
                CrawlCount = CRAWL_COUNT,
                State = JobState.Cancelled,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
            });

            await context.SaveChangesAsync();

            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);
            Assert.AreEqual(DATA_COUNT, result.DataCount);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateCompleted, result.DateCompleted);
            Assert.AreEqual(dateStarted, result.DateStarted);
            Assert.AreEqual(dateCompleted - dateStarted, result.Duration);

            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(JobState.Cancelled, result.State);
        }

        [TestMethod]
        public async Task Returns_Empty_Crawl_Result_With_Queued_Status_If_Crawl_Is_Queued()
        {
            const string ID = "random id";
            var dateQueued = new DateTime(2021, 01, 01);
            var nowProvider = new NowProvider();
            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = ID,
                JobJson = JsonConvert.SerializeObject(new StoppableCrawlJob()),
                DateQueued = dateQueued,
                State = JobState.Queued
            });

            await context.SaveChangesAsync();

            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);
            
            Assert.IsFalse(result.Data.Any());
            Assert.AreEqual(0, result.CrawlCount);
            Assert.AreEqual(default, result.Duration);
            
            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.IsNull(result.DateCompleted);
            Assert.IsNull(result.DateStarted);
            
            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(JobState.Queued, result.State);
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
            var nowProvider = new NowProvider(new DateTime(2022, 01, 02));

            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = ID,
                DateQueued = dateQueued,
                DateStarted = dateStarted,
                CrawlCount = CRAWL_COUNT,
                State = JobState.Running,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
            });

            await context.SaveChangesAsync();
            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateStarted, result.DateStarted);
            Assert.AreEqual(nowProvider.Now - dateStarted, result.Duration);
            Assert.IsNull(result.DateCompleted);

            Assert.AreEqual(0, result.Errors.Count());
            Assert.AreEqual(JobState.Running, result.State);
        }

        [TestMethod]
        public async Task Returns_Complete_Crawl_Error_Result_With_Error_Status_If_Crawl_Ran_But_Errored()
        {
            const string ID = "random id";
            const int CRAWL_COUNT = 2;
            const string DATA_FIRST_KEY = "http://localhost/";
            const string DATA_FIRST_VALUE = "data";
            const string ERROR = "something bad";
            
            var dateQueued = new DateTime(2021, 01, 01);
            var dateStarted = new DateTime(2022, 01, 01);
            var dateCompleted = new DateTime(2023, 01, 01);
            var nowProvider = new NowProvider();
            
            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = DATA_FIRST_KEY,
                        Value = DATA_FIRST_VALUE
                    }
                },
                DateStarted = dateStarted,
                DateCompleted = dateCompleted,
                DateQueued = dateQueued,
                CrawlCount = CRAWL_COUNT,
                JobErrors = new List<JobError>
                {
                    new JobError
                    {
                        Message = ERROR,
                    }
                },
                Id = ID,
                State = JobState.Errored
            });

            await context.SaveChangesAsync();

            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(ID, result.Id);

            Assert.AreEqual(DATA_FIRST_KEY, result.Data.First().Key.AbsoluteUri);
            Assert.AreEqual(DATA_FIRST_VALUE, result.Data.First().Value.First());

            Assert.AreEqual(CRAWL_COUNT, result.CrawlCount);

            Assert.AreEqual(dateQueued, result.DateQueued);
            Assert.AreEqual(dateCompleted, result.DateCompleted);
            Assert.AreEqual(dateStarted, result.DateStarted);
            Assert.AreEqual(dateCompleted - dateStarted, result.Duration);

            Assert.AreEqual(ERROR, result.Errors.First());
            Assert.AreEqual(JobState.Errored, result.State);
        }

        [TestMethod]
        public async Task Returns_Error_If_Crawl_Id_Does_Not_Match_A_Queued_Or_Completed_Crawl()
        {
            const string ID = "random id";
            var nowProvider = new NowProvider();
            
            var request = new GetCrawlRequest(ID);

            await using var context = Setup.CreateContext();

            var handler = new GetCrawlHandler(context, CreateMapper(nowProvider));

            var result = await handler.Handle(request, CancellationToken.None);

            var error = result.ErrorOrDefault;
            
            Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode);
            Assert.AreEqual("Crawl not found", error.Message);
        }
    }
}
