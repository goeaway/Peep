using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.BrowserAdapter;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Core - Crawler - PuppeteerSharp Browser Adapter")]
    public class BrowserAdapterTests
    {
        [TestMethod]
        public async Task Returns_String_UserAgent()
        {
            using var adapter = new PuppeteerSharpBrowserAdapter(1);

            var result = await adapter.GetUserAgentAsync();

            Assert.IsNotNull(result);
            Assert.AreNotEqual(0, result.Length);
        }

        [TestMethod]
        public void Returns_PageAdapterList_Equal_In_Length_To_Count_Parameter()
        {
            const int PAGE_COUNT = 2;
            
            using var adapter = new PuppeteerSharpBrowserAdapter(PAGE_COUNT);

            var result = adapter.GetPageAdapters();
            
            Assert.AreEqual(PAGE_COUNT, result.Count());
        }

        [TestMethod]
        public void Ctor_Throws_When_PageCount_Less_Than_One()
        {
            const int PAGE_COUNT = 0;

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                using var adapter = new PuppeteerSharpBrowserAdapter(PAGE_COUNT);
            });
        }
        
        [TestMethod]
        public void Ctor_Throws_When_PageCount_Greater_Than_Ten()
        {
            const int PAGE_COUNT = 11;

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            {
                using var adapter = new PuppeteerSharpBrowserAdapter(PAGE_COUNT);
            });
        }
    }
}