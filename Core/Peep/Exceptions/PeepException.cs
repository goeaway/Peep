﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Peep.Exceptions
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
