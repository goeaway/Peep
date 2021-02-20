using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.Crawler
{
    [TestCategory("Crawler - Worker Service")]
    [TestClass]
    public class WorkerServiceTests
    {
        [TestMethod]
        public async Task Stops_When_Cancellation_Token_Cancelled()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Checks_Job_Queue_For_Job()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Uses_Crawler_With_Job_Config_Update_Count_Filter_Queue_Cancellation_Token()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Pushes_Data_To_DataSink_When_Channel_Updates()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Pushes_Data_To_DataSink_When_CrawlerRunException_Thrown()
        {
            Assert.Fail();
        }
    }
}
