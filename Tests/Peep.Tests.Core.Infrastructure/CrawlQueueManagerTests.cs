using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Queuing;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Crawl Queue Manager")]
    public class CrawlQueueManagerTests
    {
        [TestMethod]
        public async Task Enqueue_Adds_All_Items_To_Queue()
        {
            var uris = new List<Uri>
            {
                new Uri("http://localhost"),
                new Uri("http://localhost/2")
            };
            
            var redis = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();

            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);

            var manager = new CrawlQueueManager(redis.Object, new CachingOptions());

            await manager.Enqueue(uris);
            
            databaseMock
                .Verify(
                    mock => mock.ListRightPushAsync(
                        It.IsAny<RedisKey>(),
                        It.Is<RedisValue[]>(
                            value => value.All(v => uris
                                .Select(u => u.AbsoluteUri).Contains(v.ToString()))),
                        It.IsAny<When>(),
                        It.IsAny<CommandFlags>()
                    ), Times.Once());
        }

        [TestMethod]
        public async Task Clear_Removes_All_Queue_Items()
        {
            var redis = new Mock<IConnectionMultiplexer>();
            var serverMock = new Mock<IServer>();

            redis
                .Setup(
                    mock => mock.GetServer(
                        It.IsAny<string>(),
                        null))
                .Returns(serverMock.Object);

            var manager = new CrawlQueueManager(redis.Object, new CachingOptions());

            await manager.Clear();
            
            serverMock
                .Verify(
                    mock => mock.FlushDatabaseAsync(
                        It.IsAny<int>(),
                        It.IsAny<CommandFlags>()
                    ), Times.Once());
        }
    }
}