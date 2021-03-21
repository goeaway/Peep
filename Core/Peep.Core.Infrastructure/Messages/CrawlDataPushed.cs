using System;
using System.Collections;
using System.Collections.Generic;

namespace Peep.Core.Infrastructure.Messages
{
    public class CrawlDataPushed
    {
        public string JobId { get; set; }
        public ExtractedData Data { get; set; }
    }
}