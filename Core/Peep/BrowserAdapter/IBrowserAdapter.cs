using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Peep.BrowserAdapter
{
    public interface IBrowserAdapter : IDisposable
    {
        Task<string> GetUserAgentAsync();
        IEnumerable<IPageAdapter> GetPageAdapters();
    }
}