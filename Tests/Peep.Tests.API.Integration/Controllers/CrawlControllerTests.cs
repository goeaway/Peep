using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.Core;
using Peep.Tests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Tests.API.Integration.Controllers
{
    [TestClass]
    [TestCategory("API - Integration - Crawl Controller")]
    public class CrawlControllerTests
    {
        [TestMethod]
        public async Task Returns_200_For_Successful_Queue_With_Crawl_Id()
        {
            var (server, client) = Setup.CreateServer();

            var job = new CrawlJob
            {

            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(200, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<QueueCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual("1", content.CrawlId);
        }

        [TestMethod]
        public async Task Returns_400_For_Request_Validation_Failure()
        {
            var (server, client) = Setup.CreateServer();

            var job = new CrawlJob
            {

            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(400, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<ErrorResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual("Validation error", content.Message);
            Assert.AreEqual("At least one seed Uri is required", content.Errors.First());
        }

        [TestMethod]
        public async Task Returns_404_For_CrawlId_Not_Found()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_With_Crawl_Still_In_Queue_Info()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_With_Crawl_Progress_If_Crawl_Running()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_With_Crawl_Result_If_Crawl_Complete()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_When_Crawl_Cancelled_Before_Being_Run()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_When_Crawl_Cancelled_During_Run()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_When_Crawl_Cancelled_After_Run()
        {
            Assert.Fail();
        }

        [TestMethod]
        public async Task Returns_200_When_Crawl_Never_Queued()
        {
            Assert.Fail();
        }
    }
}
