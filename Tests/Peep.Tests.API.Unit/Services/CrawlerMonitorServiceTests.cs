using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.MonitorCrawlers;
using Peep.API.Application.Services;
using Serilog;

namespace Peep.Tests.API.Unit.Services
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Monitor Service")]
    public class CrawlerMonitorServiceTests
    {
        [TestMethod]
        public async Task Uses_Mediator_To_Send_MonitorCrawlersRequest()
        {
            var mediator = new Mock<IMediator>();
            var logger = new LoggerConfiguration().CreateLogger();
            
            var service = new CrawlerMonitorService(mediator.Object, logger);

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await service.StopAsync(cancellationTokenSource.Token);
            
            mediator
                .Verify(
                    mock => mock
                        .Send(
                            It.IsAny<MonitorCrawlersRequest>(),
                            It.IsAny<CancellationToken>()),
                    Times.Once());
        }
    }
}