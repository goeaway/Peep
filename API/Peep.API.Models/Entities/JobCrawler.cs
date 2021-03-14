using System;
using Peep.Core.Infrastructure;

namespace Peep.API.Models.Entities
{
    public class JobCrawler
    {
        public CrawlerId CrawlerId { get; set; }
        public string JobId { get; set; }
        public Job Job { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }
}