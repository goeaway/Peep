using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.Core.Infrastructure.Data;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Peep.Data;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Cache Crawl Data Sink")]
    public class CacheCrawlDataSinkTests
    {
        [TestMethod]
        public async Task Push_Should_Set_Key_As_JobId_Data_Count_And_Guid()
        {
            var ID = "jobid";
            var DATA = new ExtractedData();

            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisDatabase = new Mock<IDatabase>();

            redisMock
                .Setup(mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(redisDatabase.Object);

            var dataSink = new CacheCrawlDataSink(redisMock.Object);

            await dataSink.Push(ID, DATA);

            redisDatabase.Verify(
                mock => mock.StringSetAsync(
                    It.Is<RedisKey>(value => value.ToString().Contains(ID) && value.ToString().Contains(DATA.Count + "")),
                    It.IsAny<RedisValue>(),
                    default, 
                    default,
                    default
                ),
                Times.Once());
        }

        [TestMethod]
        public async Task Push_Should_Set_Value_As_Serialised_Data()
        {
            var ID = "jobid";
            var DATA = new ExtractedData
            {
                { new Uri("http://localhost"), new List<string>() }
            };

            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisDatabase = new Mock<IDatabase>();

            redisMock
                .Setup(mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(redisDatabase.Object);

            var dataSink = new CacheCrawlDataSink(redisMock.Object);

            await dataSink.Push(ID, DATA);

            redisDatabase.Verify(
                mock => mock.StringSetAsync(
                    It.IsAny<RedisKey>(),
                    It.Is<RedisValue>(value => value.ToString() == JsonConvert.SerializeObject(DATA)),
                    default,
                    default,
                    default
                ),
                Times.Once());
        }
    }
}
