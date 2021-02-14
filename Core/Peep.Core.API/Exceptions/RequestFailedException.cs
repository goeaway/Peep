using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Peep.Core.API.Exceptions
{
    public class RequestFailedException : Exception
    {
        public HttpStatusCode StatusCode { get; set; }
            = HttpStatusCode.BadRequest;

        public RequestFailedException()
        {
        }

        public RequestFailedException(string message) : base(message)
        {
        }

        public RequestFailedException(HttpStatusCode statusCode) : this("Error occurred", statusCode) { }

        public RequestFailedException(string message, HttpStatusCode statusCode) : base (message)
        {
            StatusCode = statusCode;
        }
    }
}
