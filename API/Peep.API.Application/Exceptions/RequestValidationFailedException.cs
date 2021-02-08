using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Peep.API.Application.Exceptions
{
    public class RequestValidationFailedException : Exception
    {
        public IReadOnlyCollection<ValidationFailure> Failures { get; set; }

        public RequestValidationFailedException(IReadOnlyCollection<ValidationFailure> failures)
        {
            Failures = failures;
        }

        public RequestValidationFailedException()
        {
        }

        public RequestValidationFailedException(string message) : base(message)
        {
        }

        public RequestValidationFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
