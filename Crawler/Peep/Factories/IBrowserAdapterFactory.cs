using Peep.BrowserAdapter;
using Peep.Core.BrowserAdapter;
using System.Threading.Tasks;

namespace Peep.Factories
{
    public interface IBrowserAdapterFactory
    {
        Task<IBrowserAdapter> GetBrowserAdapter();
    }
}
