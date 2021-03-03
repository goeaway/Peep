using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.Core.Infrastructure.Data;
using Peep.Data;
using StackExchange.Redis;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - Cache Crawl Error Sink")]
    public class CacheCrawlErrorSinkTests
    {
        [TestMethod]
        public async Task Push_Should_Set_Key_As_JobId_Data_Count_And_Guid()
        {
            const string ID = "jobid";
            const string MESSAGE = "message";
            const string STACK_TRACE = "stack";

            var DATA = new CrawlError
            {
                Message = MESSAGE,
                StackTrace = STACK_TRACE
            };

            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisDatabase = new Mock<IDatabase>();

            redisMock
                .Setup(mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(redisDatabase.Object);

            var dataSink = new CacheCrawlErrorSink(redisMock.Object);

            await dataSink.Push(ID, DATA);

            redisDatabase.Verify(
                mock => mock.StringSetAsync(
                    It.Is<RedisKey>(value => value.ToString().Contains(ID)),
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
            const string ID = "jobid";
            const string MESSAGE = "message";
            const string STACK_TRACE = "stack";
            var DATA = new CrawlError
            {
                Message = MESSAGE,
                StackTrace = STACK_TRACE
            };

            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisDatabase = new Mock<IDatabase>();

            redisMock
                .Setup(mock => mock.GetDatabase(It.IsAny<int>(), null))
                .Returns(redisDatabase.Object);

            var dataSink = new CacheCrawlErrorSink(redisMock.Object);

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