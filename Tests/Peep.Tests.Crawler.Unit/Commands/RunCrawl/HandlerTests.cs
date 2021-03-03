using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core.API.Providers;
using Peep.Core.Infrastructure.Data;
using Peep.Crawler.Application.Options;
using Peep.Crawler.Application.Requests.Commands.RunCrawl;
using Peep.Crawler.Application.Services;
using Peep.Data;
using Peep.Exceptions;
using Peep.Filtering;
using Peep.Queueing;
using Serilog;

namespace Peep.Tests.Crawler.Unit.Commands.RunCrawl
{
    [TestClass]
    [TestCategory("Crawler - Unit - Run Crawl Handler")]
    public class RunCrawlHandlerTests
    {
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
            
            var job = new IdentifiableCrawlJob
            {
                Id = JOB_ID
            };
            
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new RunCrawlHandler(
                logger,
                cancelTokenProvider.Object,
                crawler.Object,
                filter.Object,
                queue.Object,
                dataSink.Object,
                errorSink.Object,
                crawlerConfigOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await handler.Handle(new RunCrawlRequest() { Job = job } , cancellationTokenSource.Token);
            
            crawler.Verify(
                mock => mock.Crawl(
                        job,
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
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            var job = new IdentifiableCrawlJob
            {
                Id = JOB_ID
            };
            
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
            
            var handler = new RunCrawlHandler(
                logger,
                cancelTokenProvider.Object,
                crawler.Object,
                filter.Object,
                queue.Object,
                dataSink.Object,
                errorSink.Object,
                crawlerConfigOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await channel.Writer.WriteAsync(new CrawlProgress {Data = data});
            cancellationTokenSource.CancelAfter(200);
            
            await handler.Handle(new RunCrawlRequest { Job = job }, cancellationTokenSource.Token);

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

            var job = new IdentifiableCrawlJob 
            {
                Id = JOB_ID
            };
            
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            var handler = new RunCrawlHandler(
                logger,
                cancelTokenProvider.Object,
                crawler.Object,
                filter.Object,
                queue.Object,
                dataSink.Object,
                errorSink.Object,
                crawlerConfigOptions
            );

            var cancellationTokenSource = new CancellationTokenSource();

            await handler.Handle(new RunCrawlRequest { Job = job }, cancellationTokenSource.Token);
            
            dataSink.Verify(
                mock => mock.Push(
                    JOB_ID,
                    data
                ), 
                Times.Once());
        }

        [TestMethod]
        public async Task Crawl_Is_Cancelled_When_CancellationProviderToken_Is_Cancelled()
        {
            const string JOB_ID = "id";
            const int UPDATE_COUNT = 1;
            var PRE_CANCELLED_TOKEN_SOURCE = new CancellationTokenSource();
            PRE_CANCELLED_TOKEN_SOURCE.Cancel();
            
            var crawlerConfigOptions = new CrawlConfigOptions { ProgressUpdateDataCount = UPDATE_COUNT };
            var filter = new Mock<ICrawlFilter>();
            var queue = new Mock<ICrawlQueue>();
            var dataSink = new Mock<ICrawlDataSink<ExtractedData>>();
            var errorSink = new Mock<ICrawlDataSink<CrawlError>>();
            var cancelTokenProvider = new Mock<ICrawlCancellationTokenProvider>();
            
            cancelTokenProvider
                .Setup(
                    mock => mock.GetToken(It.IsAny<string>()))
                .Returns(PRE_CANCELLED_TOKEN_SOURCE.Token);

            var job = new IdentifiableCrawlJob
            {
                Id = JOB_ID
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
                        It.IsAny<CancellationToken>())
                )
                .Returns(Channel.CreateUnbounded<CrawlProgress>().Reader);
            
            var handler = new RunCrawlHandler(
                logger,
                cancelTokenProvider.Object,
                crawler.Object,
                filter.Object,
                queue.Object,
                dataSink.Object,
                errorSink.Object,
                crawlerConfigOptions
            );

            await handler.Handle(new RunCrawlRequest { Job = job }, CancellationToken.None);
            // if this didn't work as expected the test would never finish. Maybe not the best way to assert success...
        }
    }
}