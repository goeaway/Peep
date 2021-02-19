using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Commands.CancelCrawl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Commands.CancelCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Cancel Crawl Validation")]
    public class ValidationTests
    {
        [TestMethod]
        public void Fails_With_Null_Crawl_Id()
        {
            var request = new CancelCrawlRequest(null);

            var validator = new CancelCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.CrawlId, request);

            Assert.AreEqual("Crawl id required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Fails_With_Empty_Crawl_Id()
        {
            var request = new CancelCrawlRequest("");

            var validator = new CancelCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.CrawlId, request);

            Assert.AreEqual("Crawl id required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Passes_With_Crawl_Id()
        {
            var request = new CancelCrawlRequest("value");

            var validator = new CancelCrawlValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.CrawlId, request);
        }
    }
}
