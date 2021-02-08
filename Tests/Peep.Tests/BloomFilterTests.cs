using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Filtering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Crawler - Bloom Filter")]
    public class BloomFilterTests
    {
        [TestMethod]
        public void Contains_Returns_True_When_Item_Is_Contained()
        {
            const string TEST_VALUE = "test value";

            var filter = new BloomFilter(10);
            filter.Add(TEST_VALUE);

            Assert.IsTrue(filter.Contains(TEST_VALUE));
        }

        [TestMethod]
        public void Contains_Returns_False_When_Item_Not_Contained()
        {
            const string INCLUDED = "test value";
            const string NOT_INCLUDED = "not included";

            var filter = new BloomFilter(10);
            filter.Add(INCLUDED);

            Assert.IsFalse(filter.Contains(NOT_INCLUDED));
        }

        [TestMethod]
        public void Count_Returns_Amount_Of_Added_Items()
        {
            var filter = new BloomFilter(10);
            filter.Add("something");
            filter.Add("something else");

            Assert.AreEqual(2, filter.Count);
        }

        [TestMethod]
        public void Contains_Returns_True_For_Contained_In_Full_Filter()
        {
            const int COUNT = 1_000_000;

            var filter = new BloomFilter(COUNT);

            for(var i = 0; i < COUNT; i++)
            {
                filter.Add(i + "");
            }

            var randomIndex = new Random().Next(0, COUNT);
            Assert.IsTrue(filter.Contains(randomIndex + ""));
        }

        [TestMethod]
        public void Contains_Returns_False_For_Not_Contained_In_Full_Filter()
        {
            const int COUNT = 1_000_000;

            var filter = new BloomFilter(COUNT);

            for (var i = 0; i < COUNT; i++)
            {
                filter.Add(i + "");
            }

            Assert.IsFalse(filter.Contains(-1 + ""));
        }
    }
}
