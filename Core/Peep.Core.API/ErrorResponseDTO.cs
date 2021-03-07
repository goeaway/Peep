using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace Peep.Core.API
{
    public class ErrorResponseDTO
    {
        public string Message { get; set; }

        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; }
            = HttpStatusCode.BadRequest;
        public IEnumerable<string> Errors { get; set; }
    }
}
