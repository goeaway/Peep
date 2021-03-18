using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.TestHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.UrlFrontier.Application.Commands.Enqueue;

namespace Peep.Tests.UrlFrontier.Unit.Commands.Enqueue
{
    [TestClass]
    [TestCategory("UrlFrontier - Unit - Enqueue Validation")]
    public class ValidationTests
    {
        [TestMethod]
        public void Fails_Request_With_No_Source()
        {
            var request = new EnqueueRequest();
            
            var validator = new EnqueueValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.Source, request);
            
            Assert.AreEqual("Source uri required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Fails_Request_With_Null_Uris()
        {
            var request = new EnqueueRequest();
            
            var validator = new EnqueueValidator();

            var failures = validator.ShouldHaveValidationErrorFor(r => r.Uris, request);
            
            Assert.AreEqual("Uris array required", failures.First().ErrorMessage);
        }

        [TestMethod]
        public void Passes_Request_With_Empty_Uris()
        {
            var request = new EnqueueRequest
            {
                Uris = new List<Uri>()
            };
            
            var validator = new EnqueueValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.Uris, request);
        }

        [TestMethod]
        public void Passes_Request_With_Source()
        {
            var request = new EnqueueRequest
            {
                Source = new Uri("http://localhost")
            };
            
            var validator = new EnqueueValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.Source, request);
        }

        [TestMethod]
        public void Passes_Request_With_Uris()
        {
            var request = new EnqueueRequest
            {
                Uris = new List<Uri>
                {
                    new Uri("http://localhost")
                }
            };
            
            var validator = new EnqueueValidator();

            validator.ShouldNotHaveValidationErrorFor(r => r.Uris, request);
        }
    }
}