using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core.Infrastructure.Queuing;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Cache Crawl Queue")]
    public class CacheCrawlQueueTests
    {
        [TestMethod]
        public async Task Dequeue_Dequeues_Uri_That_Was_Enqueued()
        {
            var URI = new Uri("http://localhost");
            
            var redis = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();

            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);

            databaseMock
                .Setup(
                    mock => mock.ListLeftPopAsync(
                        It.IsAny<RedisKey>(), 
                        It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue(URI.AbsoluteUri));
            
            var queue = new CacheCrawlQueue(redis.Object);

            await queue.Enqueue(URI);

            var result = await queue.Dequeue();
            Assert.AreEqual(URI.AbsoluteUri, result.AbsoluteUri);
        }

        [TestMethod]
        public async Task Dequeue_Returns_Null_If_Nothing_To_Dequeue()
        {
            var redis = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();

            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);

            databaseMock
                .Setup(
                    mock => mock.ListLeftPopAsync(
                        It.IsAny<RedisKey>(), 
                        It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue());
            
            var queue = new CacheCrawlQueue(redis.Object);

            var result = await queue.Dequeue();
            Assert.IsNull(result);
        }
    }
}