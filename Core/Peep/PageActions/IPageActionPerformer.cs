using Peep.BrowserAdapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.PageActions
{
    public interface IPageActionPerformer
    {
        Task Perform(IPageAction pageAction, IBrowserAdapter browserAdapter);
    }
}
