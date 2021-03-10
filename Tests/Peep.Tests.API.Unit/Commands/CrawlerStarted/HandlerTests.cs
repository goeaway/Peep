using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Managers;
using Peep.API.Application.Requests.Commands.CrawlerStarted;

namespace Peep.Tests.API.Unit.Commands.CrawlerStarted
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Started Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Returns_Unit_Value()
        {
            const string JOB_ID = "id";
            const string CRAWLER_ID = "crawler";
            var request = new CrawlerStartedRequest
            {
                JobId = JOB_ID,
                CrawlerId = CRAWLER_ID
            };
            
            var crawlerManager = new Mock<ICrawlerManager>();
            var handler = new CrawlerStartedHandler(crawlerManager.Object);

            var result = await handler.Handle(request, CancellationToken.None);
            
            Assert.AreEqual(MediatR.Unit.Value, result);
        }

        [TestMethod]
        public async Task Uses_CrawlerManager_Start()
        {
            const string JOB_ID = "id";
            const string CRAWLER_ID = "crawler";
            var request = new CrawlerStartedRequest
            {
                JobId = JOB_ID,
                CrawlerId = CRAWLER_ID
            };
            
            var crawlerManager = new Mock<ICrawlerManager>();
            var handler = new CrawlerStartedHandler(crawlerManager.Object);

            await handler.Handle(request, CancellationToken.None);
            
            crawlerManager
                .Verify(mock => mock.Start(CRAWLER_ID, JOB_ID), Times.Once());
        }
    }
}