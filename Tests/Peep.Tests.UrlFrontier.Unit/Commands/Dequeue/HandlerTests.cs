using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.UrlFrontier.Application.Commands.Dequeue;

namespace Peep.Tests.UrlFrontier.Unit.Commands.Dequeue
{
    [TestClass]
    [TestCategory("UrlFrontier - Unit - Dequeue Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Returns_Item_At_Front_Of_Output_Queue()
        {
            var expectedUri = new Uri("http://localhost/"); 
            
            var request = new DequeueRequest();
            var handler = new DequeueHandler();

            var result = await handler.Handle(request, CancellationToken.None);
            
            Assert.AreEqual(expectedUri, result);
        }

        [TestMethod]
        public async Task Returns_Null_If_Nothing_In_Output_Queue()
        {
            var request = new DequeueRequest();
            var handler = new DequeueHandler();

            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsNull(result);
        }
    }
}