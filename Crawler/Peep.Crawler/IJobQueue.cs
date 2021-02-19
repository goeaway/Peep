using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Crawler
{
    public interface IJobQueue
    {
        bool TryDequeue(out IdentifiableCrawlJob job);
        void Enqueue(IdentifiableCrawlJob job);
        bool TryRemove(string id);
    }
}
