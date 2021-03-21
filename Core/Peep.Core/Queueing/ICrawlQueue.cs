using System;
using System.Threading.Tasks;

namespace Peep.Core.Queueing
{
    public interface ICrawlQueue
    {
        Task<Uri> Dequeue();
        Task Enqueue(Uri uri);
    }
}
