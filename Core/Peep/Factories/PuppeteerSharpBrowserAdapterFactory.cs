using System.Collections.Generic;
using System.Threading.Tasks;
using Peep.BrowserAdapter;

namespace Peep.Factories
{
    public class PuppeteerSharpBrowserAdapterFactory : IBrowserAdapterFactory
    {
        private readonly int _pageCount;
        private readonly bool _debug;

        public PuppeteerSharpBrowserAdapterFactory()
        {
            _pageCount = 4;
            _debug = false;
        }

        public PuppeteerSharpBrowserAdapterFactory(int pageCount) : this(pageCount, false)
        {
            
        }
        
        public PuppeteerSharpBrowserAdapterFactory(int pageCount, bool debug)
        {
            _pageCount = pageCount;
            _debug = debug;
        }
        
        public Task<IBrowserAdapter> GetBrowserAdapter()
            => Task.FromResult<IBrowserAdapter>(new PuppeteerSharpBrowserAdapter(_pageCount, _debug));
    }
}