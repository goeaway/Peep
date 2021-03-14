using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.PushCrawlData;
using Peep.API.Models.Entities;
using Peep.Data;

namespace Peep.Tests.API.Unit.Messages.PushCrawlData
{
    [TestCategory("API - Unit - Push Crawl Data Handler")]
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Saves_Data_To_Found_Job()
        {
            const string JOB_ID = "id";
            const string PUSHED_URI = "http://localhost/";
            const string PUSHED_DATA = "data";
            var pushedData = new ExtractedData
            {
                {new Uri(PUSHED_URI), new List<string> {PUSHED_DATA}}
            };

            var request = new PushCrawlDataRequest
            {
                JobId = JOB_ID,
                Data = pushedData
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlDataHandler(context);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            
            Assert.AreEqual(1, job.JobData.Count);
            Assert.AreEqual(PUSHED_URI, job.JobData.First().Source);
            Assert.AreEqual(PUSHED_DATA, job.JobData.First().Value);
        }
        
        [TestMethod]
        public async Task Existing_Data_For_Job_Is_Retained()
        {
            const string JOB_ID = "id";
            const string PUSHED_URI = "http://localhost/";
            const string PUSHED_DATA = "data";
            var pushedData = new ExtractedData
            {
                {new Uri(PUSHED_URI), new List<string> {PUSHED_DATA}}
            };

            var request = new PushCrawlDataRequest
            {
                JobId = JOB_ID,
                Data = pushedData
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                JobData = new List<JobData>
                {
                    new JobData
                    {
                        Source = "http://source.com",
                        Value = "value"
                    }
                }
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlDataHandler(context);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            
            Assert.AreEqual(2, job.JobData.Count);
        }
        
        [TestMethod]
        public async Task Returns_Unit_Response_If_Job_Found()
        {
            const string JOB_ID = "id";

            var request = new PushCrawlDataRequest
            {
                JobId = JOB_ID,
                Data = new ExtractedData()
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlDataHandler(context);

            var result = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;
            
            Assert.AreEqual(MediatR.Unit.Value, result);
        }

        [TestMethod]
        public async Task Returns_Error_Response_If_Job_Not_Found()
        {
            const string JOB_ID = "id";

            var request = new PushCrawlDataRequest
            {
                JobId = JOB_ID,
            };
            
            var context = Setup.CreateContext();

            var handler = new PushCrawlDataHandler(context);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual("Could not find job", result.Message);
        }
    }
}