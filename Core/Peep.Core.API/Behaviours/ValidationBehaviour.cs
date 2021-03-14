using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Peep.Core.API.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TResponse : class
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators, ILogger logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var ctx = new ValidationContext<TRequest>(request);
            // find all failed validations related to this request
            var failures = _validators
                .Select(v => v.Validate(ctx))
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
            {
                // log failure to logger
                var requestName = typeof(TRequest).Name;
                var errorCsv = string.Join(',', failures.Select(f => f.ErrorMessage));
                _logger.Error(
                    "Validation error(s) occurred for request {RequestName}: {ErrorCSV}",
                    requestName, 
                    errorCsv);

                var responseType = typeof(TResponse);
                // if response can handle an error flow then create an instance of the TResponse
                if (responseType.IsGenericType)
                {
                    // we reflectively create an instance of the response type
                    // we expect the response type to have a constructor that can take
                    // the error response dto as sole parameter
                    return Activator.CreateInstance(
                        responseType,
                        new HttpErrorResponse
                        {
                            Errors = failures.Select(s => s.ErrorMessage),
                            Message = "Validation error"
                        }
                    ) as TResponse;
                }
            }
            // call the next stage of the request (this could be another pipeline bit or the actual request handler)
            return await next();
        }
    }
}
