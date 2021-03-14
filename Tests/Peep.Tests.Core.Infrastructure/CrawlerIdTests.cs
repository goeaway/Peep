using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Core.Infrastructure;

namespace Peep.Tests.Core.Infrastructure
{
    [TestClass]
    [TestCategory("Core - Infrastructure - CrawlerId")]
    public class CrawlerIdTests
    {
        [TestMethod]
        public void FromMachineName_Sets_Value_As_EnvironmentMachineName()
        {
            var crawlerId = CrawlerId.FromMachineName();
            Assert.AreEqual(Environment.MachineName, crawlerId.Value);
        }

        [TestMethod]
        public void Ctor_Sets_Value_As_Parameter()
        {
            const string VALUE = "value";
            var crawlerId = new CrawlerId(VALUE);
            
            Assert.AreEqual(VALUE, crawlerId.Value);
        }
        
        [TestMethod]
        public void Ctor_Throws_If_Value_Null()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new CrawlerId(null));
        }
        
        [TestMethod]
        public void Can_Create_CrawlerId_Implicitly_From_String()
        {
            const string VALUE = "value";

            static bool Created(CrawlerId crawlerId)
            {
                return crawlerId.Value == VALUE;
            }

            var result = Created(VALUE);
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void Implicit_Creation_Throws_If_Value_Null()
        {
            static void Created(CrawlerId crawlerId)
            {
            }

            Assert.ThrowsException<ArgumentNullException>(() => Created(null as string));
        }

        [TestMethod]
        public void Equals_Returns_True_When_Two_CrawlerIds_With_Same_Value_Compared()
        {
            const string VALUE = "value1";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE);

            Assert.IsTrue(crawlerId1.Equals(crawlerId2));
        }
        
        [TestMethod]
        public void Equals_Returns_True_When_Two_CrawlerIds_With_Same_Value_Compared_If_One_Is_Boxed_Object()
        {
            const string VALUE = "value1";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE);

            Assert.IsTrue(crawlerId1.Equals(crawlerId2 as object));
        }
        
        [TestMethod]
        public void Equals_Returns_False_When_Two_CrawlerIds_With_Different_Value_Compared()
        {
            const string VALUE = "value1";
            const string VALUE2 = "value2";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE2);

            Assert.IsFalse(crawlerId1.Equals(crawlerId2));
        }
        
        [TestMethod]
        public void Equals_Returns_False_When_CrawlerId_Compared_With_Different_Object()
        {
            const string VALUE = "value1";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new object();

            Assert.IsFalse(crawlerId1.Equals(crawlerId2));
        }
        
        [TestMethod]
        public void EqualsOperator_Returns_True_When_Two_CrawlerIds_With_Same_Value_Compared()
        {
            const string VALUE = "value1";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE);

            Assert.IsTrue(crawlerId1 == crawlerId2);
        }
        
        [TestMethod]
        public void EqualsOperator_Returns_False_When_Two_CrawlerIds_With_Different_Value_Compared()
        {
            const string VALUE = "value1";
            const string VALUE2 = "value2";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE2);

            Assert.IsFalse(crawlerId1 == crawlerId2);
        }

        [TestMethod] 
        public void NotEqualsOperator_Returns_False_When_Two_CrawlerIds_With_Same_Value_Compared()
        {
            const string VALUE = "value1";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE);

            Assert.IsFalse(crawlerId1 != crawlerId2);
        }
        
        [TestMethod]
        public void EqualsOperator_Returns_True_When_Two_CrawlerIds_With_Different_Value_Compared()
        {
            const string VALUE = "value1";
            const string VALUE2 = "value2";

            var crawlerId1 = new CrawlerId(VALUE);
            var crawlerId2 = new CrawlerId(VALUE2);

            Assert.IsTrue(crawlerId1 != crawlerId2);
        }

        [TestMethod]
        public void ToString_Returns_Value()
        {
            const string VALUE = "value";
            var crawlerId = new CrawlerId(VALUE);
            
            Assert.AreEqual(VALUE, crawlerId.ToString());
        }
        
        [TestMethod]
        public void GetHashCode_Returns_Value_As_HashCode()
        {
            const string VALUE = "value";
            var crawlerId = new CrawlerId(VALUE);

            var result = crawlerId.GetHashCode();
            Assert.AreEqual(VALUE.GetHashCode(), result);
        }
    }
}