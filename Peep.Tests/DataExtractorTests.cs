using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Peep.Tests
{
    [TestClass]
    [TestCategory("Data Extractor")]
    public class DataExtractorTests
    {
        [TestMethod]
        public void ExtractData_Does_Nothing_When_Regex_Not_Loaded()
        {
            const string TEST_STRING = "";

            var extractor = new DataExtractor();

            var result = extractor.ExtractData(TEST_STRING);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ExtractData_Finds_All_Matches_In_HTML()
        {
            const string TEST_STRING = "bbbbbbbbbbbbbbbbbbbbbbbbbabbbbbbbbbbbbbbbbbbbbbbba";

            var extractor = new DataExtractor();

            extractor.LoadCustomRegexPattern("(?<namedgroup>a)", "namedgroup");

            var result = extractor.ExtractData(TEST_STRING);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("a", result.First());
            Assert.AreEqual("a", result.Last());
        }

        [TestMethod]
        public void ExtractData_Returns_Empty_Collection_If_Regex_Has_No_Matches()
        {
            const string TEST_STRING = "bbbbbbbbbbbbbbbbbbbbbbbbbabbbbbbbbbbbbbbbbbbbbbbba";

            var extractor = new DataExtractor();

            extractor.LoadCustomRegexPattern("(?<data>c)");

            var result = extractor.ExtractData(TEST_STRING);
            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ExtractData_Finds_All_Matches_In_HTML_In_All_Groups()
        {
            const string TEST_STRING = "bbbbbbbbbbbbbbbbbbbbbbbbbabbbbbbbbbbbbbbbbbbbbbbbc";

            var extractor = new DataExtractor();
            var namedGroups = new List<string>
            {
                "namedgroup",
                "namedgroup2"
            };

            extractor.LoadCustomRegexPattern("(?<namedgroup>a)|(?<namedgroup2>c)", namedGroups);

            var result = extractor.ExtractData(TEST_STRING);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual("a", result.First());
            Assert.AreEqual("c", result.Last());
        }

        [TestMethod]
        public void ExtractURIs_Returns_Empty_If_Source_Null()
        {
            const string TEST_STRING = "bbbbbbbbbbbbbbbbbbbbbbbbbabbbbbbbbbbbbbbbbbbbbbbbc";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(null, TEST_STRING);

            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ExtractURIs_Returns_Empty_If_HTML_Null()
        {
            var SOURCE = new Uri("http://localhost");

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, null);

            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ExtractURIs_Returns_Empty_If_No_URI_Matches()
        {
            var SOURCE = new Uri("http://localhost");
            const string TEST_STRING = "bbbbbbbbbbbbbbbbbbbbbbbbbabbbbbbbbbbbbbbbbbbbbbbbc";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, TEST_STRING);

            Assert.IsFalse(result.Any());
        }

        [TestMethod]
        public void ExtractURIs_Returns_Multiple_URIs()
        {
            var SOURCE = new Uri("http://localhost");
            const string TEST_STRING = "<a href='/page.html'>LINK</a><a href='/page2.html'>LINK 2</a>";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, TEST_STRING);

            Assert.AreEqual("http://localhost/page.html/", result.First().AbsoluteUri);
            Assert.AreEqual("http://localhost/page2.html/", result.Last().AbsoluteUri);
        }

        [TestMethod]
        public void ExtractURIs_Returns_URI_From_Same_Source()
        {
            var SOURCE = new Uri("http://localhost");
            const string TEST_STRING = "<a href='/page.html'>LINK</a>";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, TEST_STRING);

            Assert.AreEqual("http://localhost/page.html/", result.First().AbsoluteUri);
        }

        [TestMethod]
        public void ExtractURIs_Returns_URI_From_Different_Domain()
        {
            var SOURCE = new Uri("http://localhost");
            const string TEST_STRING = "<a href='https://example.com'>LINK</a>";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, TEST_STRING);

            Assert.AreEqual("https://example.com/", result.First().AbsoluteUri);
        }

        [TestMethod]
        public void ExtractURIs_Returns_URI_From_Different_Domain_With_DoubleSlash()
        {
            var SOURCE = new Uri("http://localhost");
            const string TEST_STRING = "<a href='//example.com'>LINK</a>";

            var extractor = new DataExtractor();
            var result = extractor.ExtractURIs(SOURCE, TEST_STRING);

            Assert.AreEqual("https://example.com/", result.First().AbsoluteUri);
        }
    }
}
