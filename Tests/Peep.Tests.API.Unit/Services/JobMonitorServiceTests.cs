using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.API.Application.Requests.Commands.MonitorJobs;
using Peep.API.Application.Services;
using Serilog;

namespace Peep.Tests.API.Unit.Services
{
    [TestClass]
    [TestCategory("API - Unit - Job Monitor Service")]
    public class JobMonitorServiceTests
    {
        [TestMethod]
        public async Task Uses_Mediator_To_Create_MonitorJobsRequest()
        {
            var mediator = new Mock<IMediator>();
            
            var service = new JobMonitorService(mediator.Object, new LoggerConfiguration().CreateLogger());
            var cancellationTokenSource = new CancellationTokenSource();
            
            await service.StartAsync(cancellationTokenSource.Token);
            await service.StopAsync(cancellationTokenSource.Token);
            
            mediator
                .Verify(
                    mock => mock
                        .Send(
                            It.IsAny<MonitorJobsRequest>(),
                            It.IsAny<CancellationToken>()
                        ),
                    Times.Once());
        }
    }
}