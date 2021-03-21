using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PlaywrightSharp;
using PlaywrightSharp.Chromium;

namespace Peep.BrowserAdapter
{
    public class PlaywrightSharpBrowserAdapter : IBrowserAdapter
    {
        private readonly IBrowser _browser;
        private readonly IPlaywright _playwright;
        private readonly IBrowserContext _context;
        private readonly string _userAgent;
        private readonly List<IPageAdapter> _pageAdapters;
        
        private const int MAX_ALLOWED_PAGES = 10;
        
        public PlaywrightSharpBrowserAdapter(int pageCount)
        {
            if (pageCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount), "At least one page is required");
            }

            if (pageCount > MAX_ALLOWED_PAGES)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount),
                    $"A maximum of {MAX_ALLOWED_PAGES} page(s) is allowed");
            }
            
            string browsersPath = null;
            string driverPath = null;
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                browsersPath = "/root/.cache/ms-playwright";
                driverPath = "/app/.playwright/unix/native/playwright.sh";
            }
            
            _playwright = Playwright
                .CreateAsync(
                    browsersPath: browsersPath,
                    driverExecutablePath: driverPath
                    )
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _browser = _playwright
                .Firefox
                .LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _userAgent = "Peep Crawler";
            
            _context = _browser
                .NewContextAsync(userAgent: _userAgent)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            
            _pageAdapters = new List<IPageAdapter>();

            for (var i = 0; i < pageCount; i++)
            {
                var page = _context.NewPageAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                _pageAdapters.Add(new PlaywrightSharpPageAdapter(page));
            }
        }

        public void Dispose()
        {
            _browser?.DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            _playwright?.Dispose();
        }

        public Task<string> GetUserAgentAsync() => Task.FromResult(_userAgent);

        public IEnumerable<IPageAdapter> GetPageAdapters() => _pageAdapters;
    }
}