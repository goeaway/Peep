using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Filtering;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Core - Crawler - Bloom Filter")]
    public class BloomFilterTests
    {
        [TestMethod]
        public async Task Contains_Returns_True_When_Item_Is_Contained()
        {
            const string TEST_VALUE = "test value";

            var filter = new BloomFilter(10);
            await filter.Add(TEST_VALUE);

            Assert.IsTrue(await filter.Contains(TEST_VALUE));
        }

        [TestMethod]
        public async Task Contains_Returns_False_When_Item_Not_Contained()
        {
            const string INCLUDED = "test value";
            const string NOT_INCLUDED = "not included";

            var filter = new BloomFilter(10);
            await filter.Add(INCLUDED);

            Assert.IsFalse(await filter.Contains(NOT_INCLUDED));
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
        public async Task Contains_Returns_True_For_Contained_In_Full_Filter()
        {
            const int COUNT = 1_000_000;

            var filter = new BloomFilter(COUNT);

            for(var i = 0; i < COUNT; i++)
            {
                await filter.Add(i + "");
            }

            var randomIndex = new Random().Next(0, COUNT);
            Assert.IsTrue(await filter.Contains(randomIndex + ""));
        }

        [TestMethod]
        public async Task Contains_Returns_False_For_Not_Contained_In_Full_Filter()
        {
            const int COUNT = 1_000_000;

            var filter = new BloomFilter(COUNT);

            for (var i = 0; i < COUNT; i++)
            {
                await filter.Add(i + "");
            }

            Assert.IsFalse(await filter.Contains(-1 + ""));
        }

        [TestMethod]
        public async Task Clear_Clears_Data()
        {
            const int COUNT = 1_000_000;

            var filter = new BloomFilter(COUNT);

            for (var i = 0; i < COUNT; i++)
            {
                await filter.Add(i + "");
            }

            await filter.Clear();

            Assert.IsFalse(await filter.Contains(0 + ""));
        }
    }
}
