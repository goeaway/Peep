using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler;
using Peep.Crawler.Options;
using Peep.Filtering;
using Peep.Queueing;
using Serilog;

namespace Peep.Tests.Crawler
{
    [TestCategory("Crawler - Worker Service")]
    [TestClass]
    public class WorkerServiceTests
    {
        [TestMethod]
        public async Task Checks_Job_Queue_For_Job()
        {
            var logger = new LoggerConfiguration().CreateLogger();
            var crawler = new Mock<ICrawler>();
            var crawlerConfigOptions = new CrawlConfigOptions();
            var filter = new Mock<ICrawlFilter>();
            var queue = new Mock<ICrawlQueue>();
            var jobQueue = new Mock<IJobQueue>();
            var dataSink = new Mock<ICrawlDataSink>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var service = new Worker(
                    logger,
                    crawler.Object,
                    crawlerConfigOptions,
                    filter.Object,
                    queue.Object,
                    jobQueue.Object,
                    dataSink.Object,
                    cancelTokenProvider.Object
                );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await Task.Delay(200);

            await service.StopAsync(cancellationTokenSource.Token);
            
            jobQueue.Verify(
                mock => mock.TryDequeue(out It.Ref<IdentifiableCrawlJob>.IsAny), 
                Times.AtLeast(1));
        }
        [TestMethod]
        public async Task Uses_Crawler_With_Job_Config_Update_Count_Filter_Queue_Cancellation_Token()
        {
            const string JOB_ID = "id";
            const int UPDATE_COUNT = 1;
            
            var logger = new LoggerConfiguration().CreateLogger();
            var crawler = new Mock<ICrawler>();
            var crawlerConfigOptions = new CrawlConfigOptions { ProgressUpdateDataCount = UPDATE_COUNT };
            var filter = new Mock<ICrawlFilter>();
            var queue = new Mock<ICrawlQueue>();
            
            var jobQueue = new Mock<IJobQueue>();

            var outedJob = new IdentifiableCrawlJob {Id = JOB_ID};
            
            jobQueue
                .Setup(mock => mock.TryDequeue(out outedJob))
                .Returns(true);
            
            var dataSink = new Mock<ICrawlDataSink>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var service = new Worker(
                logger,
                crawler.Object,
                crawlerConfigOptions,
                filter.Object,
                queue.Object,
                jobQueue.Object,
                dataSink.Object,
                cancelTokenProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            // var t = service.StartAsync(cancellationTokenSource.Token);
            //
            // cancellationTokenSource.Cancel();
            // await service.StopAsync(cancellationTokenSource.Token);
            //
            crawler.Verify(
                mock => mock.Crawl(
                        outedJob,
                        UPDATE_COUNT,
                        filter.Object,
                        queue.Object,
                        It.IsAny<CancellationToken>()
                    ), 
                Times.Once());
        }

        [TestMethod]
        public async Task Pushes_Data_To_DataSink_When_Channel_Updates()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Pushes_Data_To_DataSink_When_CrawlerRunException_Thrown()
        {
            Assert.Fail();
        }
    }
}
