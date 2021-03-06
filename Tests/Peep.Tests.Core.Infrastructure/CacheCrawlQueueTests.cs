using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core.Infrastructure.Queuing;
using RedLockNet;
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
            var uri = new Uri("http://localhost");
            
            var redis = new Mock<IConnectionMultiplexer>();
            var redisLockFactory = new Mock<IDistributedLockFactory>();
            var redisLock = new Mock<IRedLock>();
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
                .ReturnsAsync(new RedisValue(uri.AbsoluteUri));
            
            redisLockFactory
                .Setup(
                    mock => mock
                        .CreateLockAsync(
                            It.IsAny<string>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            null))
                .ReturnsAsync(redisLock.Object);

            redisLock
                .Setup(mock => mock.IsAcquired).Returns(true);
            
            var queue = new CacheCrawlQueue(redis.Object, redisLockFactory.Object);

            await queue.Enqueue(uri);

            var result = await queue.Dequeue();
            Assert.AreEqual(uri.AbsoluteUri, result.AbsoluteUri);
        }

        [TestMethod]
        public async Task Dequeue_Returns_Null_If_Nothing_To_Dequeue()
        {
            var redis = new Mock<IConnectionMultiplexer>();
            var redisLockFactory = new Mock<IDistributedLockFactory>();
            var redisLock = new Mock<IRedLock>();
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
            
            redisLockFactory
                .Setup(
                    mock => mock
                        .CreateLockAsync(
                            It.IsAny<string>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            null))
                .ReturnsAsync(redisLock.Object);

            redisLock
                .Setup(mock => mock.IsAcquired).Returns(true);
            
            var queue = new CacheCrawlQueue(redis.Object, redisLockFactory.Object);

            var result = await queue.Dequeue();
            Assert.IsNull(result);
        }
        
        [TestMethod]
        public async Task Dequeue_Returns_Null_If_Lock_Not_Acquired()
        {
            var uri = new Uri("http://localhost");
            
            var redis = new Mock<IConnectionMultiplexer>();
            var redisLockFactory = new Mock<IDistributedLockFactory>();
            var redisLock = new Mock<IRedLock>();
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
                .ReturnsAsync(new RedisValue(uri.AbsoluteUri));

            redisLockFactory
                .Setup(
                    mock => mock
                        .CreateLockAsync(
                            It.IsAny<string>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            It.IsAny<TimeSpan>(),
                            null))
                .ReturnsAsync(redisLock.Object);

            redisLock
                .Setup(mock => mock.IsAcquired).Returns(false);
            
            var queue = new CacheCrawlQueue(redis.Object, redisLockFactory.Object);

            var result = await queue.Dequeue();
            Assert.IsNull(result);
        }
    }
}