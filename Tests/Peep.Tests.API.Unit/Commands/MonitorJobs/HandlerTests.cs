using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Commands.MonitorJobs;
using Peep.API.Models.Entities;
using Peep.API.Models.Enums;
using Peep.Core.API.Options;
using Peep.Core.API.Providers;
using Serilog;

namespace Peep.Tests.API.Unit.Commands.MonitorJobs
{
    [TestClass]
    [TestCategory("API - Unit - Monitor Jobs Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Sets_All_Running_Jobs_To_Errored_And_Adds_JobError_Whose_LastHeartbeat_Was_Over_Three_Ticks_Ago()
        {
            const string JOB_1 = "id", JOB_2 = "id2";
            
            var request = new MonitorJobsRequest();

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);

            await context.Jobs.AddRangeAsync(new List<Job>
            {
                new Job
                {
                    Id = JOB_1,
                    State = JobState.Running,
                    LastHeartbeat = now.AddMilliseconds(-3001)
                },
                new Job
                {
                    Id = JOB_2,
                    State = JobState.Running,
                    LastHeartbeat = now.AddMilliseconds(-3001)
                }
            });

            await context.SaveChangesAsync();
            var monitoringOptions = new MonitoringOptions
            {
                TickSeconds = 1,
                MaxUnresponsiveTicks = 3
            };
            
            var handler = new MonitorJobsHandler(context, nowProvider, monitoringOptions, new LoggerConfiguration().CreateLogger());

            await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(2, context.Jobs.Count());
            Assert.AreEqual(2, context.Jobs.Count(j => j.State == JobState.Errored));
            Assert.AreEqual("Job was unresponsive for 3 ticks", context.Jobs.First().JobErrors.First().Message);
            Assert.AreEqual("Job was unresponsive for 3 ticks", context.Jobs.Last().JobErrors.Last().Message);
        }
        
        [TestMethod]
        public async Task Does_Not_Set_Jobs_To_Errored_And_Does_Not_Add_JobError_Whose_LastHeartbeat_Was_Under_Three_Ticks_Ago()
        {
            const string JOB_ID = "id";
            var request = new MonitorJobsRequest();

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            await context.Jobs.AddRangeAsync(new List<Job>
            {
                new Job
                {
                    Id = JOB_ID,
                    State = JobState.Running,
                    LastHeartbeat = now.AddMilliseconds(-3000)
                }
            });

            await context.SaveChangesAsync();
            var monitoringOptions = new MonitoringOptions
            {
                TickSeconds = 1,
                MaxUnresponsiveTicks = 3
            };
            
            var handler = new MonitorJobsHandler(context, nowProvider, monitoringOptions, new LoggerConfiguration().CreateLogger());

            await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(1, context.Jobs.Count());
            Assert.AreEqual(0, context.Jobs.Count(j => j.State == JobState.Errored));
            Assert.AreEqual(0, context.Jobs.First().JobErrors.Count());
        }
        
        [TestMethod]
        [DataRow(JobState.Queued)]
        [DataRow(JobState.Complete)]
        [DataRow(JobState.Errored)]
        [DataRow(JobState.Cancelled)]
        public async Task Does_Not_Set_Jobs_To_Errored_And_Does_Not_Add_JobError_For_Jobs_Not_In_Running_State(JobState jobState)
        {
            const string JOB_ID = "id";
            var request = new MonitorJobsRequest();

            await using var context = Setup.CreateContext();
            var now = new DateTime(2020, 01, 01);
            var nowProvider = new NowProvider(now);
            
            await context.Jobs.AddRangeAsync(new List<Job>
            {
                new Job
                {
                    Id = JOB_ID,
                    State = jobState,
                    LastHeartbeat = now.AddMilliseconds(-3001)
                }
            });

            await context.SaveChangesAsync();
            var monitoringOptions = new MonitoringOptions
            {
                TickSeconds = 1,
                MaxUnresponsiveTicks = 3
            };
            
            var handler = new MonitorJobsHandler(context, nowProvider, monitoringOptions, new LoggerConfiguration().CreateLogger());

            await handler.Handle(request, CancellationToken.None);

            Assert.AreEqual(1, context.Jobs.Count());
            Assert.AreEqual(1, context.Jobs.Count(j => j.State == jobState));
            Assert.AreEqual(0, context.Jobs.First().JobErrors.Count());
        }
    }
}