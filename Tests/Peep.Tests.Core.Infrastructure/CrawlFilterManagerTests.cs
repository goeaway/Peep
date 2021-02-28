using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Paramore.Brighter;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Filtering;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Crawl Filter Manager")]
    public class CrawlFilterManagerTests
    {
        [TestMethod]
        public async Task GetCount_Returns_Count_In_Database()
        {
            var serverMock = new Mock<IServer>();
            serverMock
                .Setup(
                    mock => mock.Keys(
                        It.IsAny<int>(), 
                        It.IsAny<RedisValue>(),
                        It.IsAny<int>(),
                        It.IsAny<long>(),
                        It.IsAny<int>(),
                        It.IsAny<CommandFlags>()))
                .Returns(new List<RedisKey>() { "value" });
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock
                        .GetServer(It.IsAny<string>(), null))
                .Returns(serverMock.Object);
            
            var manager = new CrawlFilterManager(redis.Object, new CachingOptions());

            var result = await manager.GetCount();
            
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public async Task Clear_Clears_Database()
        {
            var serverMock = new Mock<IServer>();
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock
                        .GetServer(It.IsAny<string>(), null))
                .Returns(serverMock.Object);
            
            var manager = new CrawlFilterManager(redis.Object, new CachingOptions());

            await manager.Clear();

            serverMock
                .Verify(
                    mock => mock.FlushDatabaseAsync(It.IsAny<int>(), It.IsAny<CommandFlags>()), Times.Once());
        }
    }
}
