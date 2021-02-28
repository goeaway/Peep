using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Models.DTOs;
using Peep.Core.API.Behaviours;
using Peep.Core.API.Exceptions;

namespace Peep.Tests.Core.API
{
    [TestClass]
    [TestCategory("Core - API - Validation Behaviour")]
    public class ValidationBehaviourTests
    {
        [TestMethod]
        public async Task Throws_RequestValidationFailedException_When_Failure_Occurs()
        {
            var request = new GetCrawlRequest(null);
            var validators = new List<IValidator<GetCrawlRequest>>
            {
                new GetCrawlValidator()
            };
            
            var behaviour = new ValidationBehaviour<GetCrawlRequest, GetCrawlResponseDTO>(validators);

            await Assert.ThrowsExceptionAsync<RequestValidationFailedException>(
                () => behaviour.Handle(
                    request, 
                    CancellationToken.None, 
                    () => Task.FromResult(new GetCrawlResponseDTO())));
        }
        
        [TestMethod]
        public async Task Calls_Next_If_No_Errors()
        {
            const string ID = "id";
            var response = new GetCrawlResponseDTO();
            var request = new GetCrawlRequest(ID);
            var validators = new List<IValidator<GetCrawlRequest>>
            {
                new GetCrawlValidator()
            };
            
            var behaviour = new ValidationBehaviour<GetCrawlRequest, GetCrawlResponseDTO>(validators);
            var result = await behaviour.Handle(
                request, 
                CancellationToken.None, 
                () => Task.FromResult(response));
            
            Assert.AreEqual(response, result);
        }
    }
}