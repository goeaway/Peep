using System.Threading.Tasks;
using Peep.BrowserAdapter;

namespace Peep.Core.PageActions
{
    public interface IPageActionPerformer
    {
        Task Perform(IPageAction pageAction, IPageAdapter pageAdapter);
    }
}
