using Peep.BrowserAdapter;
using System.Threading.Tasks;

namespace Peep.Factories
{
    public class PuppeteerSharpBrowserAdapterFactory : IBrowserAdapterFactory
    {
        Task<IBrowserAdapter> IBrowserAdapterFactory.GetBrowserAdapter() 
            => Task.FromResult<IBrowserAdapter>(new PuppeteerSharpBrowserAdapter());
    }
}
