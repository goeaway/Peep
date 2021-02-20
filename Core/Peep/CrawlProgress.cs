using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class CrawlProgress
    {
        public IDictionary<Uri, IEnumerable<string>> Data { get; set; }
    }
}
