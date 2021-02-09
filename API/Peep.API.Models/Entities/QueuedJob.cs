using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.Entities
{
    public class QueuedJob
    {
        public string Id { get; set; }
        public string JobJson { get; set; }
        public DateTime DateQueued { get; set; }
    }
}
