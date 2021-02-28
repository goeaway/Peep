using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Filtering;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Cache Crawl Filter")]
    public class CacheCrawlFilterTests
    {
        [TestMethod]
        public void Count_Returns_Count_In_Database()
        {
            const int COUNT = 1;
            var serverMock = new Mock<IServer>();
            serverMock
                .Setup(
                    mock => mock
                        .Keys(
                            It.IsAny<int>(), 
                            It.IsAny<RedisValue>(),
                            It.IsAny<int>(),
                            It.IsAny<long>(),
                            It.IsAny<int>(),
                            It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Range(0, COUNT).Select(i => new RedisKey("")));
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetServer(
                        It.IsAny<string>(),
                        null))
                .Returns(serverMock.Object);
            
            var options = new CachingOptions();
            
            var filter = new CacheCrawlFilter(redis.Object, options);

            var result = filter.Count;
            
            Assert.AreEqual(COUNT, result);
        }

        [TestMethod]
        public async Task Add_Adds_Uri_As_Key_And_Empty_Value()
        {
            const string URI = "uri";
            var databaseMock = new Mock<IDatabase>();
            databaseMock
                .Setup(
                    mock => mock
                        .StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)
                )
                .ReturnsAsync(new RedisValue(URI));
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);
            
            var options = new CachingOptions();
            
            var filter = new CacheCrawlFilter(redis.Object, options);

            await filter.Add(URI);

            databaseMock
                .Verify(mock => mock.StringSetAsync(
                    URI, 
                    "", 
                    null, 
                    When.Always, 
                    CommandFlags.FireAndForget),
                Times.Once());
        }

        [TestMethod]
        public async Task Contains_Returns_True_If_Uri_Contained()
        {
            const string URI = "uri";
            var databaseMock = new Mock<IDatabase>();
            databaseMock
                .Setup(
                    mock => mock
                        .StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)
                )
                .ReturnsAsync(new RedisValue(URI));
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);
            
            var options = new CachingOptions();
            
            var filter = new CacheCrawlFilter(redis.Object, options);

            var result = await filter.Contains(URI);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Contains_Returns_False_If_Uri_Not_Contained()
        {
            const string URI = "uri";
            var databaseMock = new Mock<IDatabase>();
            databaseMock
                .Setup(
                    mock => mock
                        .StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)
                    )
                .ReturnsAsync(new RedisValue());
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetDatabase(
                        It.IsAny<int>(),
                        null))
                .Returns(databaseMock.Object);
            
            var options = new CachingOptions();
            
            var filter = new CacheCrawlFilter(redis.Object, options);

            var result = await filter.Contains(URI);

            Assert.IsFalse(result);
        }
    }
}
