using System;
using System.Threading.Tasks;

namespace Peep.BrowserAdapter
{
    public interface IPageAdapter : IDisposable
    {
        Task<string> GetContentAsync();
        Task<bool> NavigateToAsync(Uri uri);
        Task WaitForSelector(string selector, TimeSpan timeout);
        Task Click(string selector);
        Task ScrollY(int scrollAmount);
    }
}