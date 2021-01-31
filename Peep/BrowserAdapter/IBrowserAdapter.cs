using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.BrowserAdapter
{
    public interface IBrowserAdapter : IDisposable
    {
        Task<string> GetUserAgentAsync();
        Task<string> GetContentAsync();
        Task<bool> QuerySelectorFoundAsync(string selector);
        Task<bool> NavigateToAsync(Uri uri);
    }
}
