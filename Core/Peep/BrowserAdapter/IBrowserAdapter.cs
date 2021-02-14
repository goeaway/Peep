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
        Task<bool> NavigateToAsync(Uri uri);
        Task WaitForSelector(string selector, TimeSpan timeout);
        Task Click(string selector);
        Task ScrollY(int scrollAmount);
    }
}
