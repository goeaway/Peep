using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peep.UrlFrontier.Client
{
    public class UrlFrontier : IUrlFrontier
    {
        public Task<Uri> Dequeue()
        {
            throw new NotImplementedException();
        }

        public Task Enqueue(Uri source, IEnumerable<Uri> uris)
        {
            throw new NotImplementedException();
        }
    }
}