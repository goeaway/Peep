using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Queries.GetCrawl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Queries.GetCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Get Crawl Validation")]
    public class ValidationTests
    {
        [TestMethod]
        public void Fails_With_Null_Crawl_Id()
        {
            var request = new GetCrawlRequest(null);

            var validator = new GetCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.CrawlId, request);

            Assert.AreEqual("Crawl id required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Fails_With_Empty_Crawl_Id()
        {
            var request = new GetCrawlRequest("");

            var validator = new GetCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.CrawlId, request);

            Assert.AreEqual("Crawl id required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Passes_With_Crawl_Id()
        {
            var request = new GetCrawlRequest("value");

            var validator = new GetCrawlValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.CrawlId, request);
        }
    }
}
