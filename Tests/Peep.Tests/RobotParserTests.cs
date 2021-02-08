using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Crawler - Robot Parser")]
    public class RobotParserTests
    {
        [TestMethod]
        public async Task UriForbidden_Returns_True_If_User_Agent_Forbidden_For_A_URI()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: test-user-agent
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.IsAny<HttpRequestMessage>(), 
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_True_If_URI_Included_In_Catchall_Section()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: *
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_False_If_User_Agent_Not_Forbidden_For_A_URI()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: some-user-agent
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_False_If_URI_Not_In_Robots_File()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: test-user-agent
Disallow: /some-other-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_True_If_URI_Second_User_Agent_Block()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: *
Disallow: /some-other-page

User-Agent: test-user-agent
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_False_If_Robots_File_Not_Found()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UriForbidden_Only_Requests_Robots_File_Once_For_Domain()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            const string TEST_ROBOTS = @"User-Agent: *
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result1 = await parser.UriForbidden(URI, USER_AGENT);
            var result2 = await parser.UriForbidden(URI, USER_AGENT);

            Assert.AreEqual(result1, result2);

            mockMessageHandler
                .Protected()
                .Verify(
                    "SendAsync", 
                    Times.Once(), 
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        [TestMethod]
        public async Task UriForbidden_Returns_True_If_URI_Forbidden_After_Malformed_Block()
        {
            var URI = new Uri("http://test.com/some-page");
            const string USER_AGENT = "test-user-agent";
            // mispelled important part of the first block
            const string TEST_ROBOTS = @"User-Aent: *
Disallow: /some-other-page

User-Agent: test-user-agent
Disallow: /some-page
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task UriForbidden_Returns_True_If_URI_Part_Of_Forbidden_Path()
        {
            var URI = new Uri("http://test.com/some-area/some-page");
            const string USER_AGENT = "test-user-agent";
            // mispelled important part of the first block
            const string TEST_ROBOTS = @"User-Agent: *
Disallow: /some-area
";

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(TEST_ROBOTS)
                });

            var client = new HttpClient(mockMessageHandler.Object);
            var parser = new RobotParser(client);

            var result = await parser.UriForbidden(URI, USER_AGENT);
            Assert.IsTrue(result);
        }
    }
}
