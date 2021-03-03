using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core.API.Providers;
using Peep.Crawler.Application.Requests.Commands.CancelCrawl;
using Peep.Crawler.Application.Services;

namespace Peep.Tests.Crawler.Unit.Commands.CancelCrawl
{
    [TestClass]
    [TestCategory("Crawler - Unit - Cancel Crawl Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Uses_JobQueue_TryRemove_With_CrawlId()
        {
            var CRAWL_ID = "1";
            var request = new CancelCrawlRequest { CrawlId = CRAWL_ID };
            
            var jobQueue = new Mock<IJobQueue>();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new CancelCrawlHandler(jobQueue.Object, cancellationTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);
            
            jobQueue
                .Verify(
                    mock => mock.TryRemove(CRAWL_ID),
                    Times.Once());
        }

        [TestMethod]
        public async Task Does_Not_Use_CancellationTokenProvider_CancelJob_With_CrawlId_If_JobQueue_Has_Job()
        {
            var CRAWL_ID = "1";
            var request = new CancelCrawlRequest { CrawlId = CRAWL_ID };
            
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .Setup(
                    mock => mock.TryRemove(CRAWL_ID))
                .Returns(true);
            
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new CancelCrawlHandler(jobQueue.Object, cancellationTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);
            
            cancellationTokenProvider
                .Verify(
                    mock => mock.CancelJob(CRAWL_ID),
                    Times.Never());
        }

        
        [TestMethod]
        public async Task Uses_CancellationTokenProvider_CancelJob_With_CrawlId_If_JobQueue_Doesnt_Have_Job()
        {
            var CRAWL_ID = "1";
            var request = new CancelCrawlRequest { CrawlId = CRAWL_ID };
            
            var jobQueue = new Mock<IJobQueue>();
            jobQueue
                .Setup(
                    mock => mock.TryRemove(CRAWL_ID))
                .Returns(false);
            
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new CancelCrawlHandler(jobQueue.Object, cancellationTokenProvider.Object);

            await handler.Handle(request, CancellationToken.None);
            
            cancellationTokenProvider
                .Verify(
                    mock => mock.CancelJob(CRAWL_ID),
                    Times.Once());
        }

        [TestMethod]
        public async Task Returns_UnitValue()
        {
            var CRAWL_ID = "1";
            var request = new CancelCrawlRequest { CrawlId = CRAWL_ID };
            
            var jobQueue = new Mock<IJobQueue>();
            var cancellationTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new CancelCrawlHandler(jobQueue.Object, cancellationTokenProvider.Object);

            var result = await handler.Handle(request, CancellationToken.None);
            
            Assert.AreEqual(MediatR.Unit.Value, result);
        }
    }
}