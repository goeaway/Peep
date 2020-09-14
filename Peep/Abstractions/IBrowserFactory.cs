using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Abstractions
{
    public interface IBrowserFactory
    {
        Task<Browser> GetBrowser();
    }
}
