using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Commands.QueueCrawl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Unit.Commands.QueueCrawl
{
    [TestClass]
    [TestCategory("API - Unit - Queue Crawl Validation")]
    public class ValidationTests
    {
        [TestMethod]
        public void Fails_When_Job_Null()
        {
            var request = new QueueCrawlRequest(null);
            var validator = new QueueCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.Job, request);

            Assert.AreEqual("Job required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Fails_When_Jobs_Seeds_Null()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = null
            };
            var request = new QueueCrawlRequest(job);
            var validator = new QueueCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.Job.Seeds, request);

            Assert.AreEqual("At least 1 seed uri is required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Fails_When_Jobs_Seeds_Empty()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>()
            };
            var request = new QueueCrawlRequest(job);
            var validator = new QueueCrawlValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.Job.Seeds, request);

            Assert.AreEqual("At least 1 seed uri is required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Passes_When_Jobs_Seeds_Has_One()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };
            var request = new QueueCrawlRequest(job);
            var validator = new QueueCrawlValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.Job.Seeds, request);
        }
    }
}
