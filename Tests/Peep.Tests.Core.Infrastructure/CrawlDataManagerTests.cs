using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Peep.Core.API.Options;
using Peep.Core.Infrastructure.Data;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Crawl Data Manager")]
    public class CrawlDataManagerTests
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
            
            var manager = new CrawlDataSinkManager(redis.Object, cachingOptions);

            var result = await manager.GetCount(JOB_ID);
            
            Assert.AreEqual(FULL_COUNT, result);
        }

        [TestMethod]
        public async Task GetData_Returns_All_Data_For_Job()
        {
            const string JOB_ID = "id";

            var data1 = new Dictionary<Uri, IEnumerable<string>>
            {
                { new Uri("http://localhost/"), new List<string> { "data" } }
            };

            var data2 = new Dictionary<Uri, IEnumerable<string>>
            {
                { new Uri("http://localhost/1"), new List<string> { "data1" } },
                { new Uri("http://localhost/2"), new List<string> { "data2" } }
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
            
            var manager = new CrawlDataSinkManager(redis.Object, cachingOptions);

            var result = await manager.GetData(JOB_ID);
            
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("http://localhost/", result.Keys.First().AbsoluteUri);
            Assert.AreEqual("data", result.Values.First().First());
        }

        [TestMethod]
        public async Task Clear_Clears_All_Data_For_Job()
        {
            Assert.Fail();
        }
    }
}
