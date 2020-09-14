using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Abstractions
{
    public interface IBloomFilter
    {
        int Count { get; }
        void Add(string uri);
        bool Contains(string uri);
    }
}
