using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Tests.Crawler
{
    [TestClass]
    [TestCategory("Crawler - Job Queue")]
    public class JobQueueTests
    {
        [TestMethod]
        public void TryDequeue_Returns_True_And_Outs_First_Enqueued_Job()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void TryDequeue_Returns_False_If_Nothing_In_Queue()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void TryRemove_Returns_False_If_Nothing_Found()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void TryRemove_Throws_If_Id_Null()
        {
            Assert.Fail();
        }
        
        [TestMethod]
        public void TryRemove_Returns_True_And_Removes_If_Found()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void Enqueue_Throws_If_Job_Null()
        {
            Assert.Fail();
        }
    }
}
