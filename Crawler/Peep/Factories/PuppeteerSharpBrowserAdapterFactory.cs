using Peep.BrowserAdapter;
using Peep.Core.BrowserAdapter;
using System.Threading.Tasks;

namespace Peep.Factories
{
    public class PuppeteerSharpBrowserAdapterFactory : IBrowserAdapterFactory
    {
        Task<IBrowserAdapter> IBrowserAdapterFactory.GetBrowserAdapter() 
            => Task.FromResult<IBrowserAdapter>(new PuppeteerSharpBrowserAdapter());
    }
}
