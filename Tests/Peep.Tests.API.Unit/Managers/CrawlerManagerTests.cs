using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Managers;

namespace Peep.Tests.API.Unit.Managers
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Manager")]
    public class CrawlerManagerTests
    {
        [TestMethod]
        public void Finish_Throws_If_No_CrawlerId_Job_Combo_Found()
        {
            const string CRAWLER_ID = "crawler";
            const string DIFFERENT_CRAWLER_ID = "different crawler";
            const string JOB_ID = "job";
            const string DIFFERENT_JOB_ID = "different job";
            
            var manager = new CrawlerManager();

            manager.Start(CRAWLER_ID, JOB_ID);
            Assert.ThrowsException<InvalidOperationException>(() => manager.Finish(DIFFERENT_CRAWLER_ID, JOB_ID));
            Assert.ThrowsException<InvalidOperationException>(() => manager.Finish(CRAWLER_ID, DIFFERENT_JOB_ID));
        }

        [TestMethod]
        public async Task WaitAllFinished_Completes_If_No_Crawlers_Started()
        {
            const string JOB_ID = "job";
            
            var manager = new CrawlerManager();

            await manager.WaitAllFinished(JOB_ID, TimeSpan.MinValue);
        }
        
        [TestMethod]
        public async Task WaitAllFinished_Waits_Completes_When_All_Started_Crawlers_For_Job_Are_Finished()
        {
            const string CRAWLER_ID = "crawler";
            const string DIFFERENT_CRAWLER_ID = "different crawler";
            const string JOB_ID = "job";

            var manager = new CrawlerManager();

            manager.Start(CRAWLER_ID, JOB_ID);
            manager.Start(DIFFERENT_CRAWLER_ID, JOB_ID);
            
            manager.Finish(CRAWLER_ID, JOB_ID);
            manager.Finish(DIFFERENT_CRAWLER_ID, JOB_ID);

            await manager.WaitAllFinished(JOB_ID, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public async Task WaitAllFinished_Throws_If_Any_Started_Crawler_Does_Not_Finish_Before_Timeout()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var manager = new CrawlerManager();

            manager.Start(CRAWLER_ID, JOB_ID);

            await Assert.ThrowsExceptionAsync<TimeoutException>(
                () => manager.WaitAllFinished(JOB_ID, TimeSpan.FromMilliseconds(1)));
        }

        [TestMethod]
        public void Clear_Removes_All_Data_For_Job()
        {
            const string CRAWLER_ID = "crawler";
            const string JOB_ID = "job";

            var manager = new CrawlerManager();

            manager.Start(CRAWLER_ID, JOB_ID);

            manager.Clear(JOB_ID);

            Assert.ThrowsException<InvalidOperationException>(() => manager.Finish(CRAWLER_ID, JOB_ID));
        }
    }
}