using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Queueing
{
    public interface ICrawlQueue
    {
        Task<Uri> Dequeue();
        Task Enqueue(Uri uri);
        Task Enqueue(IEnumerable<Uri> uris);
        Task Clear();
    }
}
