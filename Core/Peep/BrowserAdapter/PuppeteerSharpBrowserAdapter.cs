using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Peep.BrowserAdapter
{
    public class PuppeteerSharpBrowserAdapter : IBrowserAdapter
    {
        private readonly Browser _browser;
        private readonly List<IPageAdapter> _pageAdapters;

        private const int MAX_ALLOWED_PAGE_COUNT = 10;
        
        public PuppeteerSharpBrowserAdapter(int pageCount) : this(pageCount, false) {}
        
        public PuppeteerSharpBrowserAdapter(int pageCount, bool debug)
        {
            if (pageCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount), "At least one page is required");
            }

            if (pageCount > MAX_ALLOWED_PAGE_COUNT)
            {
                throw new ArgumentOutOfRangeException($"A maximum of {MAX_ALLOWED_PAGE_COUNT} page(s) is allowed");
            }
            
            // we can't just use the downloadable browser on linux (in docker)
            // if the OS is linux, puppeteer sharp will pick up the browser location
            // set in the environment variable PUPPETEER_EXECUTABLE_PATH 
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                new BrowserFetcher()
                    .DownloadAsync(BrowserFetcher.DefaultRevision)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            
            _browser = Puppeteer
                .LaunchAsync(new LaunchOptions
                {
                    Headless = !debug,
                    Args = new[]
                    {
                        "--no-sandbox"
                    }
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _pageAdapters = new List<IPageAdapter>();

            for (var i = 0; i < pageCount; i++)
            {
                // puppeteersharp will automatically create a page, so use that instead of creating an extra one
                var page = i == 0
                    ? _browser.PagesAsync().ConfigureAwait(false).GetAwaiter().GetResult().First()
                    : _browser.NewPageAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                _pageAdapters.Add(new PuppeteerSharpPageAdapter(page));
            }
        }

        public void Dispose()
        {
            // foreach (var adapter in _pageAdapters)
            // {
            //     adapter.Dispose();
            // }
            
            _browser.Dispose();
        }

        public Task<string> GetUserAgentAsync() => _browser.GetUserAgentAsync();
        public IEnumerable<IPageAdapter> GetPageAdapters() => _pageAdapters;
    }
}