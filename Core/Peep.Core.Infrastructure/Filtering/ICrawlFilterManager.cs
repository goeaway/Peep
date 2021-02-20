using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Filtering
{
    public interface ICrawlFilterManager
    {
        Task<int> GetCount();
        Task Clear();
    }
}
