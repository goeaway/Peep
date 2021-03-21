using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Core;
using Peep.Crawler.Application.Services;

namespace Peep.Tests.Crawler.Unit.Services
{
    [TestClass]
    [TestCategory("Crawler - Unit - Job Queue")]
    public class JobQueueTests
    {
        [TestMethod]
        public void TryDequeue_Returns_True_And_Outs_First_Enqueued_Job()
        {
            const string ID_1 = "id1";
            const string ID_2 = "id2";
            
            var jobQueue = new JobQueue();
            
            jobQueue.Enqueue(new IdentifiableCrawlJob {Id = ID_1});
            jobQueue.Enqueue(new IdentifiableCrawlJob {Id = ID_2});

            var result = jobQueue.TryDequeue(out var job);
            
            Assert.IsTrue(result);
            Assert.AreEqual(ID_1, job.Id);
        }

        [TestMethod]
        public void TryDequeue_Returns_False_If_Nothing_In_Queue()
        {
            var jobQueue = new JobQueue();

            var result = jobQueue.TryDequeue(out var job);

            Assert.IsFalse(result);
            Assert.IsNull(job);
        }

        [TestMethod]
        public void TryRemove_Returns_False_If_Nothing_Found()
        {
            const string ID = "id";
            var jobQueue = new JobQueue();

            var result = jobQueue.TryRemove(ID);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryRemove_Throws_If_Id_Null()
        {
            var jobQueue = new JobQueue();

            Assert.ThrowsException<ArgumentNullException>(() => jobQueue.TryRemove(null));
        }
        
        [TestMethod]
        public void TryRemove_Returns_True_And_Removes_If_Found()
        {
            const string ID = "id";
            var jobQueue = new JobQueue();
            
            jobQueue.Enqueue(new IdentifiableCrawlJob { Id = ID });

            var result = jobQueue.TryRemove(ID);

            var dequeueResult = jobQueue.TryDequeue(out var job);

            Assert.IsTrue(result);
            Assert.IsFalse(dequeueResult);
            Assert.IsNull(job);
        }

        [TestMethod]
        public void Enqueue_Throws_If_Job_Null()
        {
            var jobQueue = new JobQueue();
            
            Assert.ThrowsException<ArgumentNullException>(() => jobQueue.Enqueue(null));
        }
    }
}
