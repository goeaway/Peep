using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Peep.BrowserAdapter
{
    public class PuppeteerSharpPageAdapter : IPageAdapter
    {
        private readonly Page _page;

        public PuppeteerSharpPageAdapter(Page page)
        {
            _page = page;
        }

        public void Dispose() => _page.Dispose();
        
        public Task<string> GetContentAsync() => _page.GetContentAsync();
        
        public async Task<bool> NavigateToAsync(Uri uri)
        {
            if(uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var response = await _page.GoToAsync(uri.AbsoluteUri, WaitUntilNavigation.DOMContentLoaded);
            return response.Ok;
        }

        public async Task WaitForSelector(string selector, TimeSpan timeout)
        {
            await _page.WaitForSelectorAsync(selector, new WaitForSelectorOptions 
            { 
                Timeout = (int)timeout.TotalMilliseconds 
            });
        }

        public async Task Click(string selector)
        {
            if(selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            await _page.ClickAsync(selector);
        }

        public async Task ScrollY(int scrollAmount)
        {
            await _page.EvaluateExpressionAsync($"window.scrollBy(0, {scrollAmount})");
        }
    }
}