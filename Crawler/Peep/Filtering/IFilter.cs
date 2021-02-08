using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Filtering
{
    public interface IFilter
    {
        int Count { get; }
        void Add(string uri);
        bool Contains(string uri);
    }
}
