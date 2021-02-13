using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Peep.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.DTOs
{
    public class GetCrawlResponseDTO
    {
        public string Id { get; set; }
        public IDictionary<Uri, IEnumerable<string>> Data { get; set; }
        public TimeSpan Duration { get; set; }
        public int CrawlCount { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public CrawlState State { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime DateQueued { get; set; }
        public DateTime? DateStarted { get; set; }
        public DateTime? DateCompleted { get; set; }
    }
}
