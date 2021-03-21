using System;
using System.Threading.Tasks;
using PlaywrightSharp;

namespace Peep.BrowserAdapter
{
    public class PlaywrightSharpPageAdapter : IPageAdapter
    {
        private readonly IPage _page;

        public PlaywrightSharpPageAdapter(IPage page)
        {
            _page = page;
        }

        public void Dispose()
        {
            
        }

        public Task<string> GetContentAsync() => _page.GetContentAsync();

        public async Task<bool> NavigateToAsync(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            
            var result = await _page.GoToAsync(uri.AbsoluteUri, 
                LifecycleEvent.Load,
                timeout: (int)TimeSpan.FromMinutes(2).TotalMilliseconds);
            return result.Ok;
        }

        public async Task WaitForSelector(string selector, TimeSpan timeout)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            
            await _page.WaitForSelectorAsync(selector, WaitForState.Visible, (int)timeout.TotalMilliseconds);
        }

        public async Task Click(string selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            await _page.ClickAsync(selector);
        }

        public async Task ScrollY(int scrollAmount)
        {
            await _page.EvaluateAsync($"window.scrollBy(0, {scrollAmount}");
        }
    }
}