using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Integration.Controllers
{
    [TestClass]
    [TestCategory("API - Integration - Queue Controller")]
    public class QueueControllerTests
    {
        [TestMethod]
        public async Task Returns_200_With_List_Of_Queued_Crawls()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_With_Empty_List_Of_Crawls()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_With_List_Of_Queued_Crawls_In_Order_They_Were_Added()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_For_Delete_Crawl_When_Crawl_In_Queue()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_404_For_Delete_Crawl_When_Crawl_Not_In_Queue()
        {
            Assert.Fail();
        }
    }
}
