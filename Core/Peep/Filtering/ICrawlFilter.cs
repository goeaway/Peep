using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Filtering
{
    public interface ICrawlFilter
    {
        int Count { get; }
        Task Add(string uri);
        Task<bool> Contains(string uri);
    }
}
