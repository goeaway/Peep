using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;

namespace Peep.API.Models.Entities
{
    public class Job
    {
        public Job()
        {
            JobData = new List<JobData>();
            JobErrors = new List<JobError>();
        }
        
        public string Id { get; set; }
        public string JobJson { get; set; }
        public DateTime DateQueued { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
        public int CrawlCount { get; set; }
        public JobState State { get; set; }
        public List<JobData> JobData { get; set; }
        public List<JobError> JobErrors { get; set; }
        
        public DateTime LastHeartbeat { get; set; }
    }
}
