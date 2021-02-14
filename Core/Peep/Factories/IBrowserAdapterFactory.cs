using Peep.BrowserAdapter;
using System.Threading.Tasks;

namespace Peep.Factories
{
    public interface IBrowserAdapterFactory
    {
        Task<IBrowserAdapter> GetBrowserAdapter();
    }
}
