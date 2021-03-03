using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Core.API;

namespace Peep.Tests.Core.API
{
    [TestClass]
    [TestCategory("Core - API - Service Extensions")]
    public class ServiceExtensionsTests
    {
        [TestMethod]
        public void CachingOptions_Bind_From_Configuration()
        {
            const string EXPECTED_HOSTNAME = "host";
            const int EXPECTED_PORT = 1;
            
            var service = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("Resources\\cachingOptions.json")
                .Build();
            
            service.AddCachingOptions(configuration, out var options);
            
            Assert.AreEqual(EXPECTED_HOSTNAME, options.Hostname);
            Assert.AreEqual(EXPECTED_PORT, options.Port);
        }

        [TestMethod]
        public void MessagingOptions_Bind_From_Configuration()
        {
            const string EXPECTED_HOSTNAME = "host";
            const int EXPECTED_PORT = 1;
            const string EXPECTED_USERNAME = "user";
            const string EXPECTED_PASSWORD = "password";
            
            var service = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("Resources\\messagingOptions.json")
                .Build();
            
            service.AddMessagingOptions(configuration, out var options);
            
            Assert.AreEqual(EXPECTED_HOSTNAME, options.Hostname);
            Assert.AreEqual(EXPECTED_PORT, options.Port);
            Assert.AreEqual(EXPECTED_USERNAME, options.Username);
            Assert.AreEqual(EXPECTED_PASSWORD, options.Password);
        }
    }
}