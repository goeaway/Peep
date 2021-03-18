using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peep.UrlFrontier.Client
{
    public interface IUrlFrontier
    {
        Task<Uri> Dequeue();
        Task Enqueue(Uri source, IEnumerable<Uri> uris);
    }
}