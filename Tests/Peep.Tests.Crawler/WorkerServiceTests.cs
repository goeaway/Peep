using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Threading.Channels;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler;
using Peep.Crawler.Options;
using Peep.Data;
using Peep.Exceptions;
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
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var service = new Worker(
                    logger,
                    crawler.Object,
                    crawlerConfigOptions,
                    filter.Object,
                    queue.Object,
                    jobQueue.Object,
                    dataSink.Object,
                    errorSink.Object,
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
            
            // using .Setup here causes the worker service to deadlock, 
            // I think this issue is caused by the below, not sure how .SetupSequence fixes it though...
            // https://github.com/dotnet/runtime/issues/36063#issuecomment-559130348
            jobQueue
                .SetupSequence(mock => mock.TryDequeue(out outedJob)) 
                .Returns(true);
            
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var service = new Worker(
                logger,
                crawler.Object,
                crawlerConfigOptions,
                filter.Object,
                queue.Object,
                jobQueue.Object,
                dataSink.Object,
                errorSink.Object,
                cancelTokenProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            
            await service.StopAsync(cancellationTokenSource.Token);
            
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
            const string JOB_ID = "id";
            const int UPDATE_COUNT = 1;
            var crawlerConfigOptions = new CrawlConfigOptions { ProgressUpdateDataCount = UPDATE_COUNT };
            var filter = new Mock<ICrawlFilter>();
            var queue = new Mock<ICrawlQueue>();
            var jobQueue = new Mock<IJobQueue>();
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var outedJob = new IdentifiableCrawlJob {Id = JOB_ID};
            var logger = new LoggerConfiguration().CreateLogger();

            var channel = Channel.CreateUnbounded<CrawlProgress>();

            var data = new ExtractedData
            {
                {new Uri("http://localhost/"), new List<string> {"data"}}
            };
            
            var crawler = new Mock<ICrawler>();
            crawler
                .Setup(
                    mock => mock.Crawl(
                        It.IsAny<IdentifiableCrawlJob>(),
                        It.IsAny<int>(),
                        It.IsAny<ICrawlFilter>(),
                        It.IsAny<ICrawlQueue>(),
                        It.IsAny<CancellationToken>())
                )
                .Returns(channel.Reader);
            
            // using .Setup here causes the worker service to deadlock, 
            // I think this issue is caused by the below, not sure how .SetupSequence fixes it though...
            // https://github.com/dotnet/runtime/issues/36063#issuecomment-559130348
            jobQueue
                .SetupSequence(mock => mock.TryDequeue(out outedJob)) 
                .Returns(true);
            
            var service = new Worker(
                logger,
                crawler.Object,
                crawlerConfigOptions,
                filter.Object,
                queue.Object,
                jobQueue.Object,
                dataSink.Object,
                errorSink.Object,
                cancelTokenProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);

            await channel.Writer.WriteAsync(new CrawlProgress {Data = data});
            
            await service.StopAsync(cancellationTokenSource.Token);
            
            // verify data sink was called when channel reader is updated
            dataSink
                .Verify(
                    mock => mock.Push(JOB_ID, data), Times.Once());
        }

        [TestMethod]
        public async Task Pushes_Data_To_DataSink_When_CrawlerRunException_Thrown()
        {
            const string JOB_ID = "id";
            const int UPDATE_COUNT = 1;

            var data = new ExtractedData
            {
                {new Uri("http://localhost/"), new List<string> {"data"}}
            };
            
            var logger = new LoggerConfiguration().CreateLogger();
            var crawler = new Mock<ICrawler>();
            crawler
                .Setup(
                    mock => mock.Crawl(
                        It.IsAny<IdentifiableCrawlJob>(), 
                        It.IsAny<int>(), 
                        It.IsAny<ICrawlFilter>(), 
                        It.IsAny<ICrawlQueue>(), 
                        It.IsAny<CancellationToken>()))
                .Throws(new CrawlerRunException("error", new CrawlProgress { Data = data }));
            var crawlerConfigOptions = new CrawlConfigOptions { ProgressUpdateDataCount = UPDATE_COUNT };
            var filter = new Mock<ICrawlFilter>();
            var queue = new Mock<ICrawlQueue>();
            
            var jobQueue = new Mock<IJobQueue>();

            var outedJob = new IdentifiableCrawlJob {Id = JOB_ID};
            
            // using .Setup here causes the worker service to deadlock, 
            // I think this issue is caused by the below, not sure how .SetupSequence fixes it though...
            // https://github.com/dotnet/runtime/issues/36063#issuecomment-559130348
            jobQueue
                .SetupSequence(mock => mock.TryDequeue(out outedJob)) 
                .Returns(true);
            
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var service = new Worker(
                logger,
                crawler.Object,
                crawlerConfigOptions,
                filter.Object,
                queue.Object,
                jobQueue.Object,
                dataSink.Object,
                errorSink.Object,
                cancelTokenProvider.Object
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await service.StartAsync(cancellationTokenSource.Token);
            
            await service.StopAsync(cancellationTokenSource.Token);
            
            dataSink.Verify(
                mock => mock.Push(
                    JOB_ID,
                    data
                ), 
                Times.Once());
        }
    }
}
