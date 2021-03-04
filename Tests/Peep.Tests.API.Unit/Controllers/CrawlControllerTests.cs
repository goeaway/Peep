using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using Peep.API.Application.Requests.Commands.QueueCrawl;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Controllers;

namespace Peep.Tests.API.Unit.Controllers
{
    [TestClass]
    [TestCategory("API - Unit - Crawl Controller")]
    public class CrawlControllerTests
    {
        [TestMethod]
        public async Task Get_Uses_Mediator()
        {
            const string CRAWL_ID = "id";
            
            var mediator = new Mock<IMediator>();
            
            var controller = new CrawlController(mediator.Object);

            await controller.Get(CRAWL_ID);
            
            mediator
                .Verify(
                    mock => 
                        mock.Send(
                            It.Is<GetCrawlRequest>(value => value.CrawlId == CRAWL_ID),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Queue_Uses_Mediator()
        {
            var job = new StoppableCrawlJob();
            
            var mediator = new Mock<IMediator>();
            
            var controller = new CrawlController(mediator.Object);

            await controller.Queue(job);
            
            mediator
                .Verify(
                    mock => 
                        mock.Send(
                            It.Is<QueueCrawlRequest>(value => value.Job == job),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
        
        [TestMethod]
        public async Task Cancel_Uses_Mediator()
        {
            const string CRAWL_ID = "id";
            
            var mediator = new Mock<IMediator>();
            
            var controller = new CrawlController(mediator.Object);

            await controller.Cancel(CRAWL_ID);
            
            mediator
                .Verify(
                    mock => 
                        mock.Send(
                            It.Is<CancelCrawlRequest>(value => value.CrawlId == CRAWL_ID),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    }
}