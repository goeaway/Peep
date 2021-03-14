using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.PushCrawlError;
using Peep.API.Models.Entities;

namespace Peep.Tests.API.Unit.Messages.PushCrawlError
{
    [TestCategory("API - Unit - Push Crawl Error Handler")]
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Saves_Error_To_Found_Job()
        {
            const string JOB_ID = "id";
            const string MESSAGE = "message";
            const string STACK_TRACE = "stack";
            const string SOURCE = "source";

            var request = new PushCrawlErrorRequest()
            {
                JobId = JOB_ID,
                Message = MESSAGE,
                Source = SOURCE,
                StackTrace = STACK_TRACE
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlErrorHandler(context);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            
            Assert.AreEqual(1, job.JobErrors.Count);
            Assert.AreEqual(SOURCE, job.JobErrors.First().Source);
            Assert.AreEqual(MESSAGE, job.JobErrors.First().Message);
            Assert.AreEqual(STACK_TRACE, job.JobErrors.First().StackTrace);
        }
        
        [TestMethod]
        public async Task Existing_Errors_For_Job_Is_Retained()
        {
            const string JOB_ID = "id";
            const string MESSAGE = "message";
            const string STACK_TRACE = "stack";
            const string SOURCE = "source";

            var request = new PushCrawlErrorRequest
            {
                JobId = JOB_ID,
                Message = MESSAGE,
                StackTrace = STACK_TRACE,
                Source = SOURCE
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID,
                JobErrors = new List<JobError>
                {
                    new JobError
                    {
                        Message = "other error"
                    }
                }
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlErrorHandler(context);

            await handler.Handle(request, CancellationToken.None);

            var job = context.Jobs.First();
            
            Assert.AreEqual(2, job.JobErrors.Count);
        }
        
        [TestMethod]
        public async Task Returns_Unit_Response_If_Job_Found()
        {
            const string JOB_ID = "id";
            const string MESSAGE = "message";
            const string STACK_TRACE = "stack";
            const string SOURCE = "source";
            
            var request = new PushCrawlErrorRequest
            {
                JobId = JOB_ID,
                Message = MESSAGE,
                Source = SOURCE,
                StackTrace = STACK_TRACE
            };
            
            var context = Setup.CreateContext();

            await context.Jobs.AddAsync(new Job
            {
                Id = JOB_ID
            });

            await context.SaveChangesAsync();
            
            var handler = new PushCrawlErrorHandler(context);

            var result = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;
            
            Assert.AreEqual(MediatR.Unit.Value, result);
        }

        [TestMethod]
        public async Task Returns_Error_Response_If_Job_Not_Found()
        {
            const string JOB_ID = "id";

            var request = new PushCrawlErrorRequest
            {
                JobId = JOB_ID,
            };
            
            var context = Setup.CreateContext();

            var handler = new PushCrawlErrorHandler(context);

            var result = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual("Could not find job", result.Message);
        }
    }
}