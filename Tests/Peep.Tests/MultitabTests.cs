using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Peep.Factories;
using PuppeteerSharp;

namespace Peep.Tests
{
    [TestCategory("Experimental")]
    [TestClass]
    public class MultitabTests
    {
        [TestMethod]
        public async Task Can_Work_With_Task_Based_Multi_Tabbed_Browser()
        {
            await new BrowserFetcher()
                .DownloadAsync(BrowserFetcher.DefaultRevision);

            await using var browser = await Puppeteer
                .LaunchAsync(new LaunchOptions
                {
                    Headless = false,
                    Args = new[]
                    {
                        "--no-sandbox"
                    }
                });

            await using var page1 = (await browser.PagesAsync()).First();
            await using var page2 = await browser.NewPageAsync();
            await using var page3 = await browser.NewPageAsync();
            await using var page4 = await browser.NewPageAsync();
            
            await page1.GoToAsync("https://google.com");
            await page2.GoToAsync("http://youtube.com");
            await page3.GoToAsync("http://bbc.co.uk");
            await page4.GoToAsync("http://old.reddit.com");

            var pageTaskDict = 
                (await browser.PagesAsync())
                .ToDictionary(k => k, v => (Task)null);
            
            var pageTaskDictKeys = pageTaskDict.Keys.ToList();
            
            var start = DateTime.Now;
            var timeoutRandomiser = new Random();
            
            while (DateTime.Now - start < TimeSpan.FromMinutes(1))
            {
                foreach (var page in pageTaskDictKeys)
                {
                    if (pageTaskDict[page] != null)
                    {
                        continue;
                    }
                    
                    var reloadTask = page
                        .ReloadAsync()
                        .ContinueWith(async antecedent =>
                        {
                            Console.WriteLine(page.GetTitleAsync());
                            await Task.Delay(timeoutRandomiser.Next(500, 2000));
                            pageTaskDict[page] = null;
                        });
                    pageTaskDict[page] = reloadTask;
                }
            }
        }
    }
}