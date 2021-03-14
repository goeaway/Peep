using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Messages.CrawlerDown;
using Peep.API.Models.Entities;

namespace Peep.Tests.API.Unit.Messages.CrawlerDown
{
    [TestClass]
    [TestCategory("API - Unit - Crawler Down Handler")]
    public class HandlerTests
    {
        [TestMethod]
        public async Task Removes_JobCrawler_With_Id_Provided()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerDownRequest
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID,
            });

            await context.SaveChangesAsync();
            
            var handler = new CrawlerDownHandler(context);

            var response = await handler.Handle(request, CancellationToken.None);
            
            Assert.AreEqual(0, context.JobCrawlers.Count());
        }
        
        [TestMethod]
        public async Task Returns_Unit_Value_When_Successful()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerDownRequest
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            await context.JobCrawlers.AddAsync(new JobCrawler
            {
                CrawlerId = CRAWLER_ID,
            });

            await context.SaveChangesAsync();
            
            var handler = new CrawlerDownHandler(context);

            var response = (await handler.Handle(request, CancellationToken.None)).SuccessOrDefault;
            
            Assert.AreEqual(MediatR.Unit.Value, response);
        }

        [TestMethod]
        public async Task Returns_ErrorResponse_If_JobCrawler_Not_Found()
        {
            const string CRAWLER_ID = "id";

            var request = new CrawlerDownRequest
            {
                CrawlerId = CRAWLER_ID
            };

            await using var context = Setup.CreateContext();

            var handler = new CrawlerDownHandler(context);

            var response = (await handler.Handle(request, CancellationToken.None)).ErrorOrDefault;
            
            Assert.AreEqual("Crawler not found", response.Message);
        }
    }
}