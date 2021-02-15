using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler.Models.DTOs
{
    public class CrawlDataDTO
    {
        public int Count { get; set; }
        public bool Complete { get; set; }
        public string ErrorMessage { get; set; }
        public IDictionary<Uri, IEnumerable<string>> Data { get; set; }
            = new Dictionary<Uri, IEnumerable<string>>();
    }
}
