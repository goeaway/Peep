using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using Peep.Core;

namespace Peep.API.Models.DTOs
{
    public class GetCrawlResponseDto
    {
        public string Id { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public JobState State { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime DateQueued { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
        public int DataCount { get; set; }
        public int CrawlCount { get; set; }

        public IEnumerable<string> Errors { get; set; }
            = new List<string>();
        public ExtractedData Data { get; set; }
    }
}
