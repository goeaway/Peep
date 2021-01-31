using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Filtering
{
    public interface IBloomFilter
    {
        int Count { get; }
        void Add(string uri);
        bool Contains(string uri);
    }
}
