using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Peep.BrowserAdapter;

namespace Peep.Factories
{
    public interface IBrowserAdapterFactory
    {
        Task<IBrowserAdapter> GetBrowserAdapter();
    }
}