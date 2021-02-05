using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Peep.BrowserAdapter
{
    public class PuppeteerSharpBrowserAdapter : IBrowserAdapter
    {
        private readonly Browser _browser;
        private readonly Page _page;

        public PuppeteerSharpBrowserAdapter()
        {
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
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox"
                    }
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            _page = _browser
                .PagesAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .First();
        }

        private void Bash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        public void Dispose() => _browser.Dispose();
        public Task<string> GetContentAsync() => _page.GetContentAsync();
        public Task<string> GetUserAgentAsync() => _browser.GetUserAgentAsync();
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
