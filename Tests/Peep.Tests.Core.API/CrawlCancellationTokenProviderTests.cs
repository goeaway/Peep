using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Core.API.Providers;

namespace Peep.Tests.Core.API
{
    [TestClass]
    [TestCategory("Core - API - Crawl Cancellation Token Provider")]
    public class CrawlCancellationTokenProviderTests
    {
        [TestMethod]
        public void GetToken_Returns_Existing_Token_If_Found()
        {
            const string ID = "id";
            
            var provider = new CrawlCancellationTokenProvider();

            var token = provider.GetToken(ID);
            var token2 = provider.GetToken(ID);
            
            Assert.AreEqual(token, token2);
        }

        [TestMethod]
        public void GetToken_Returns_New_Token_If_Not_Found()
        {
            const string ID = "id";
            
            var provider = new CrawlCancellationTokenProvider();

            var token = provider.GetToken(ID);
            
            Assert.IsNotNull(token);
        }

        [TestMethod]
        public void CancelJob_Returns_False_If_No_Token_Created()
        {
            const string ID = "id";
            
            var provider = new CrawlCancellationTokenProvider();

            var result = provider.CancelJob(ID);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CancelJob_Returns_True_If_Token_Found_Subsequent_Calls_Return_True()
        {
            const string ID = "id";
            
            var provider = new CrawlCancellationTokenProvider();

            provider.GetToken(ID);
            var result = provider.CancelJob(ID);
            var result2 = provider.CancelJob(ID);

            Assert.IsTrue(result);
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public void DisposeOfToken_Removes_Token_And_Returns_True()
        {
            const string ID = "id";
            var provider = new CrawlCancellationTokenProvider();

            provider.GetToken(ID);

            var result = provider.DisposeOfToken(ID);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void DisposeOfToken_Returns_False_If_No_Token_Exists()
        {
            const string ID = "id";
            var provider = new CrawlCancellationTokenProvider();

            var result = provider.DisposeOfToken(ID);
            Assert.IsFalse(result);
        }
    }
}