using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Peep.API.Models.DTOs;
using Peep.API.Models.Entities;
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
        public async Task Queue_Returns_200_For_Successful_Queue_With_Crawl_Id()
        {
            var (_, client) = Setup.CreateServer();

            var job = new CrawlJob
            {
                Seeds = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(200, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<QueueCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.IsFalse(string.IsNullOrWhiteSpace(content.CrawlId));
        }

        [TestMethod]
        public async Task Queue_Returns_400_For_Request_Validation_Failure()
        {
            var (_, client) = Setup.CreateServer();

            var job = new CrawlJob();

            var requestContent = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/crawl", requestContent);

            Assert.AreEqual(400, (int)response.StatusCode);

            var content = JsonConvert.DeserializeObject<ErrorResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual("Validation error", content.Message);
            Assert.AreEqual("At least 1 seed uri is required", content.Errors.First());
        }

        [TestMethod]
        public async Task Get_Returns_404_For_CrawlId_Not_Found()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client) = Setup.CreateServer();

            var response = await client.GetAsync($"/crawl/{CRAWL_ID}");

            Assert.AreEqual(404, (int)response.StatusCode);
        }

        [TestMethod]
        public async Task Get_Returns_200_With_Crawl_Still_In_Queue_Info()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client, context) = Setup.CreateDataBackedServer();

            context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            context.SaveChanges();

            var response = await client.GetAsync($"/crawl/{CRAWL_ID}");

            response.EnsureSuccessStatusCode();

            var responseContent =
                JsonConvert.DeserializeObject<GetCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.IsNull(responseContent.DateStarted);
        }

        [TestMethod]
        public async Task Get_Returns_200_With_Crawl_Progress_If_Crawl_Running()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client, context) = Setup.CreateDataBackedServer();

            context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            context.SaveChanges();

            var response = await client.GetAsync($"/crawl/{CRAWL_ID}");

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

            var (_, client, context) = Setup.CreateDataBackedServer();

            context.CompletedJobs.Add(new CompletedJob
            {
                Id = CRAWL_ID,
                DateCompleted = DATE_COMPLETED
            });

            context.SaveChanges();

            var response = await client.GetAsync($"/crawl/{CRAWL_ID}");

            response.EnsureSuccessStatusCode();

            var responseContent = JsonConvert.DeserializeObject<GetCrawlResponseDTO>(await response.Content.ReadAsStringAsync());

            Assert.AreEqual(DATE_COMPLETED, responseContent.DateCompleted);
        }

        [TestMethod]
        public async Task Cancel_Returns_200_When_Crawl_Cancelled_Before_Being_Run()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client, context) = Setup.CreateDataBackedServer();

            context.QueuedJobs.Add(new QueuedJob
            {
                Id = CRAWL_ID
            });

            var response = await client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            response.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task Cancel_Returns_200_When_Crawl_Cancelled_During_Run()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client, context) = Setup.CreateDataBackedServer();

            context.RunningJobs.Add(new RunningJob
            {
                Id = CRAWL_ID
            });

            var response = await client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            response.EnsureSuccessStatusCode();
        }

        [TestMethod]
        public async Task Cancel_Returns_404_When_Crawl_Not_Queued_Or_Running()
        {
            const string CRAWL_ID = "crawl-id";

            var (_, client) = Setup.CreateServer();

            var response = await client.PostAsync($"/crawl/cancel/{CRAWL_ID}", new StringContent(""));

            Assert.AreEqual(404, (int)response.StatusCode);
        }
    }
}
