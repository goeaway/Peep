using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.API.Application.Requests.Queries.GetCrawl;
using Peep.API.Models.DTOs;
using Peep.Core.API;
using Peep.Core.API.Behaviours;
using Serilog;

namespace Peep.Tests.Core.API
{
    [TestClass]
    [TestCategory("Core - API - Validation Behaviour")]
    public class ValidationBehaviourTests
    {
        private readonly ILogger _logger = new LoggerConfiguration().CreateLogger();
        
        [TestMethod]
        public async Task Returns_ErrorResponseDTO_When_Validation_Fails()
        {
            var request = new GetCrawlRequest(null);
            var validators = new List<IValidator<GetCrawlRequest>>
            {
                new GetCrawlValidator()
            };
            
            var behaviour = new ValidationBehaviour<GetCrawlRequest, Either<GetCrawlResponseDto, HttpErrorResponse>>(validators, _logger);

            var result = await behaviour.Handle(
                request, 
                CancellationToken.None,
                () => Task
                    .FromResult(
                        new Either<GetCrawlResponseDto, HttpErrorResponse>(new GetCrawlResponseDto())));
            var error = result.ErrorOrDefault;

            Assert.IsNotNull(error);
            Assert.AreEqual(HttpStatusCode.BadRequest, error.StatusCode);
            Assert.AreEqual("Validation error", error.Message);
        }
        
        [TestMethod]
        public async Task Calls_Next_If_No_Errors()
        {
            const string ID = "id";
            var response = new GetCrawlResponseDto();
            var request = new GetCrawlRequest(ID);
            var validators = new List<IValidator<GetCrawlRequest>>
            {
                new GetCrawlValidator()
            };
            
            var behaviour = new ValidationBehaviour<GetCrawlRequest, GetCrawlResponseDto>(validators, _logger);
            
            var result = await behaviour.Handle(
                request, 
                CancellationToken.None, 
                () => Task.FromResult(response));
            
            Assert.AreEqual(response, result);
        }
    }
}