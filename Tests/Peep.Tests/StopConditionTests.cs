using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.StopConditions;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Core - Crawler - Stop Conditions")]
    public class StopConditionTests
    {
        [TestMethod]
        public void Stop_Throws_If_Value_Null()
        {
            var stopCondition = new SerialisableStopCondition();
            var crawlResult = new CrawlResult();

            Assert.ThrowsException<InvalidOperationException>(() => stopCondition.Stop(crawlResult));
        }
        
        [TestMethod]
        public void Stop_Throws_If_CrawlResult_Null()
        {
            var stopCondition = new SerialisableStopCondition
            {
                Value = new object()
            };

            Assert.ThrowsException<ArgumentNullException>(() => stopCondition.Stop(null));
        }

        [TestMethod]
        [DataRow(1, 1, true)]
        [DataRow(1, 2, true)]
        [DataRow(1, 0, false)]
        public void Stop_Returns_Correct_Result_If_Type_MaxCrawlCount(
            int stopValue, int crawlCount, bool shouldStop)
        {
            var stopCondition = new SerialisableStopCondition
            {
                Value = stopValue,
                Type = SerialisableStopConditionType.MaxCrawlCount
            };

            var result = stopCondition.Stop(new CrawlResult {CrawlCount = crawlCount});

            Assert.AreEqual(shouldStop, result);
        }
        
        [TestMethod]
        [DataRow(1, 1, true)]
        [DataRow(1, 2, true)]
        [DataRow(1, 0, false)]
        public void Stop_Returns_Correct_Result_If_Type_MaxDataCount(
            int stopValue, int dataCount, bool shouldStop)
        {
            var stopCondition = new SerialisableStopCondition
            {
                Value = stopValue,
                Type = SerialisableStopConditionType.MaxDataCount
            };

            var result = stopCondition.Stop(new CrawlResult {DataCount = dataCount});

            Assert.AreEqual(shouldStop, result);
        }
        
        [TestMethod]
        [DataRow(1, 1, true)]
        [DataRow(1, 2, true)]
        [DataRow(1, 0, false)]
        public void Stop_Returns_Correct_Result_If_Type_MaxDurationSeconds(
            int stopValue, int crawlDurationSeconds, bool shouldStop)
        {
            var stopCondition = new SerialisableStopCondition
            {
                Value = stopValue,
                Type = SerialisableStopConditionType.MaxDurationSeconds
            };

            var result = stopCondition.Stop(new CrawlResult {Duration = TimeSpan.FromSeconds(crawlDurationSeconds)});

            Assert.AreEqual(shouldStop, result);
        }
    }
}