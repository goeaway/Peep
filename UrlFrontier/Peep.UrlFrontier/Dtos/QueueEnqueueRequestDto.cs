using System;
using System.Collections.Generic;

namespace Peep.UrlFrontier.Dtos
{
    public class QueueEnqueueRequestDto
    {
        public Uri Source { get; set; }
        public IEnumerable<Uri> Uris { get; set; }
    }
}