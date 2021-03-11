using Peep.BrowserAdapter;
using System.Threading.Tasks;

namespace Peep.Factories
{
    public class PuppeteerSharpBrowserAdapterFactory : IBrowserAdapterFactory
    {
        public Task<IBrowserAdapter> GetBrowserAdapter() 
            => Task.FromResult<IBrowserAdapter>(new PuppeteerSharpBrowserAdapter());
    }
}
