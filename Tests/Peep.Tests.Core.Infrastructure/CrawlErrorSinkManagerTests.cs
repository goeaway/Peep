using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Data;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Crawl Error Manager")]
    public class CrawlErrorSinkManagerTests
    {
        [TestMethod]
        public async Task GetCount_Returns_Combined_Amount_From_All_Relevant_Keys()
        {
            const int FULL_COUNT = 3;
            const int FIRST_COUNT = 1;
            const int SECOND_COUNT = 2;
            
            const string JOB_ID = "id";

            var mockServer = new Mock<IServer>();
            mockServer
                .Setup(
                    mock => mock
                        .Keys(
                            It.IsAny<int>(),
                            It.IsAny<RedisValue>(),
                            It.IsAny<int>(),
                            It.IsAny<long>(),
                            It.IsAny<int>(),
                            It.IsAny<CommandFlags>()))
                .Returns(new List<RedisKey> { $"{JOB_ID}.id.{FIRST_COUNT}", $"{JOB_ID}.id.{SECOND_COUNT}" });
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetServer(
                        It.IsAny<string>(), 
                        null))
                .Returns(mockServer.Object);
            
            var cachingOptions = new CachingOptions();
            
            var manager = new CrawlErrorSinkManager(redis.Object, cachingOptions);

            var result = await manager.GetCount(JOB_ID);
            
            Assert.AreEqual(FULL_COUNT, result);
        }

        [TestMethod]
        public async Task GetData_Returns_All_Data_For_Job()
        {
            const string JOB_ID = "id";

            var data1 = new CrawlErrors
            {
                new CrawlError()
            };

            var data2 = new CrawlErrors
            {
                new CrawlError(),
                new CrawlError()
            };

            var mockDatabase = new Mock<IDatabase>();
            mockDatabase
                .Setup(
                    mock => mock.StringGetAsync(
                        It.IsAny<RedisKey[]>(),
                        It.IsAny<CommandFlags>()))
                .ReturnsAsync(new RedisValue[] { JsonConvert.SerializeObject(data1), JsonConvert.SerializeObject(data2) });
            
            var mockServer = new Mock<IServer>();
            mockServer
                .Setup(
                    mock => mock
                        .Keys(
                            It.IsAny<int>(),
                            It.IsAny<RedisValue>(),
                            It.IsAny<int>(),
                            It.IsAny<long>(),
                            It.IsAny<int>(),
                            It.IsAny<CommandFlags>()))
                .Returns(new RedisKey[] { "1", "2" });
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetServer(
                        It.IsAny<string>(), 
                        null))
                .Returns(mockServer.Object);

            redis
                .Setup(
                    mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(mockDatabase.Object);
            
            var cachingOptions = new CachingOptions();
            
            var manager = new CrawlErrorSinkManager(redis.Object, cachingOptions);

            var result = await manager.GetData(JOB_ID);
            
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod]
        public async Task Clear_Clears_All_Data_For_Job()
        {
            const string JOB_ID = "id";

            var mockDatabase = new Mock<IDatabase>();
            var mockServer = new Mock<IServer>();
            var redisKeys = new RedisKey[] { "1", "2" };
            
            mockServer
                .Setup(
                    mock => mock
                        .Keys(
                            It.IsAny<int>(),
                            It.IsAny<RedisValue>(),
                            It.IsAny<int>(),
                            It.IsAny<long>(),
                            It.IsAny<int>(),
                            It.IsAny<CommandFlags>()))
                .Returns(redisKeys);
            
            var redis = new Mock<IConnectionMultiplexer>();
            redis
                .Setup(
                    mock => mock.GetServer(
                        It.IsAny<string>(), 
                        null))
                .Returns(mockServer.Object);

            redis
                .Setup(
                    mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(mockDatabase.Object);
            
            var cachingOptions = new CachingOptions();
            
            var manager = new CrawlErrorSinkManager(redis.Object, cachingOptions);

            await manager.Clear(JOB_ID);
            
            // verify key delete async is called for each key
            mockDatabase
                .Verify(
                    mock => mock.KeyDeleteAsync(
                        It.IsIn(redisKeys), It.IsAny<CommandFlags>()), Times.Exactly(2));
        }
    }
}