using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core;
using Peep.Crawler.Application.Requests.Commands.QueueCrawl;
using Peep.Crawler.Application.Services;

namespace Peep.Tests.Crawler.Unit.Commands.QueueCrawl
{
    [TestClass]
    [TestCategory("Crawler - Unit - Queue Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Uses_JobQueue_Enqueue()
        {
            var job = new IdentifiableCrawlJob();
            
            var request = new QueueCrawlRequest { Job = job };
            
            var jobQueue = new Mock<IJobQueue>();
            
            var handler = new QueueCrawlHandler(jobQueue.Object);

            await handler.Handle(request, CancellationToken.None);
            
            jobQueue
                .Verify(
                    mock => mock.Enqueue(job),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Returns_UnitValue()
        {
            var job = new IdentifiableCrawlJob();
            
            var request = new QueueCrawlRequest { Job = job };
            
            var jobQueue = new Mock<IJobQueue>();
            
            var handler = new QueueCrawlHandler(jobQueue.Object);

            var result = 
                (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;

            Assert.AreEqual(MediatR.Unit.Value, result);
        }
    }
}