using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.DTOs
{
    public class ErrorResponseDTO
    {
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; }
    }
}
