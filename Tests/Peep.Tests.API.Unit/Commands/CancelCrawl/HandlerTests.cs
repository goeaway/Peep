using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Commands.CancelCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Cancel Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Throws_If_Crawl_Not_Queued_Or_Running()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Dequeues_Crawl_If_Queued()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Cancels_Crawl_If_Running()
        {
            Assert.Fail();
        }
    }
}
