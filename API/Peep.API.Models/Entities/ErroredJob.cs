using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.Entities
{
    public class ErroredJob
    {
        public string Id { get; set; }
        public string JobJson { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime DateQueued { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateCompleted { get; set; }
        public int CrawlCount { get; set; }
        public TimeSpan Duration { get; set; }
        public string DataJson { get; set; }
    }
}
