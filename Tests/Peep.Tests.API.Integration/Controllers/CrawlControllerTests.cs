using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Peep.API.Application.Options;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
using Peep.API.Persistence;
using Peep.Core.API.Providers;
using Peep.Tests.API.Core;
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
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly PeepApiContext _context;

        public CrawlControllerTests()
        {
            (_server, _client, _context) = Setup.CreateDataBackedServer();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _server.Dispose();
            _client.Dispose();
            _context.Dispose();
        }

        [TestMethod]
        public async Task Queue_Returns_200_For_Successful_Queue_With_Crawl_Id()
        {
            var job = new StoppableCrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(200, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<QueueCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.IsFalse(string.IsNullOrWhiteSpace(content.CrawlId));
        }

        [TestMethod]
        public async Task Queue_Returns_400_For_Request_Validation_Failure()
        {
            var job = new StoppableCrawlJob();

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(400, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<ErrorResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual("Validation error", content.Message);
            Assert.AreEqual("At least 1 seed uri is required", content.Errors.First());
        }

        [TestMethod]
        public async Task Get_Returns_404_For_CrawlId_Not_Found()
        {
            const string CRAWL_ID = "crawl-id";

            var response = await _client.GetAsync($"/crawl/{CRAWL_ID}");

            Assert.AreEqual(404, (int)response.StatusCode);
        }

        [TestMethod]
        public async Task Get_Returns_200_With_Crawl_Still_In_Queue_Info()
        {
            const string CRAWL_ID = "crawl-id";

            _context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            _context.SaveChanges();

            var response = await _client.GetAsync($"/crawl/{CRAWL_ID}");

            response.EnsureSuccessStatusCode();

            var responseContent =
                JsonConvert.DeserializeObject<GetCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.IsNull(responseContent.DateStarted);
        }

        [TestMethod]
        public async Task Get_Returns_200_With_Crawl_Progress_If_Crawl_Running()
        {
            const string CRAWL_ID = "crawl-id";

            _context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            _context.SaveChanges();

            var response = await _client.GetAsync($"/crawl/{CRAWL_ID}");

            response.EnsureSuccessStatusCode();

            var responseContent = 
                JsonConvert.DeserializeObject<GetCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.IsNull(responseContent.DateCompleted);
        }

        [TestMethod]
        public async Task Get_Returns_200_With_Crawl_Result_If_Crawl_Complete()
        {
            const string CRAWL_ID = "crawl-id";
            var DATE_COMPLETED = new DateTime(2020, 01, 01);

            _context.CompletedJobs.Add(new CompletedJob
            {
                Id = CRAWL_ID,
                DateCompleted = DATE_COMPLETED
            });

            _context.SaveChanges();

            var response = await _client.GetAsync($"/crawl/{CRAWL_ID}");

            response.EnsureSuccessStatusCode();

            var responseContent = JsonConvert.DeserializeObject<GetCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual(DATE_COMPLETED, responseContent.DateCompleted);
        }

        [TestMethod]
        public async Task Cancel_Returns_200_When_Crawl_Cancelled_Before_Being_Run()
        {
            const string CRAWL_ID = "crawl-id";

            _context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            var response = await _client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            response.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task Cancel_Returns_200_When_Crawl_Cancelled_During_Run()
        {
            const string CRAWL_ID = "crawl-id";

            var mockTokenProvider = new Mock<ICrawlCancellationTokenProvider>();

            mockTokenProvider.Setup(mock => mock.CancelJob(CRAWL_ID)).Returns(true);

            var (_, client) = Setup.CreateServer(new Setup.CreateServerOptions
            {
                TokenProvider = mockTokenProvider.Object
            });

            var response = await client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            response.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task Cancel_Returns_404_When_Crawl_Not_Queued_Or_Running()
        {
            const string CRAWL_ID = "crawl-id";

            var response = await _client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            Assert.AreEqual(404, (int)response.StatusCode);
        }
    }
}
