using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Models.DTOs
{
    public class GetCrawlResponseDTO
    {
        // could be a crawl result, crawl progress or still in queue info

        public IDictionary<Uri, IEnumerable<string>> Data { get; set; }
        public TimeSpan Duration { get; set; }
        public int CrawlCount { get; set; }
        public string Id { get; set; }
    }
}
