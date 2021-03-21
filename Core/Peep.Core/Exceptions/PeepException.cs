using System;

namespace Peep.Core.Exceptions
{
    public class PeepException : Exception
    {
        public PeepException()
        {
        }

        public PeepException(string message) : base(message)
        {
        }

        public PeepException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
