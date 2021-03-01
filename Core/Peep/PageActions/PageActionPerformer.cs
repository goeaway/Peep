using Peep.BrowserAdapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.PageActions
{
    public class PageActionPerformer : IPageActionPerformer
    {
        public async Task Perform(IPageAction pageAction, IBrowserAdapter browserAdapter)
        {
            if (pageAction == null)
            {
                throw new ArgumentNullException(nameof(pageAction));
            }
            
            if (browserAdapter == null)
            {
                throw new ArgumentNullException(nameof(browserAdapter));
            }

            switch (pageAction.Type)
            {
                case SerialisablePageActionType.Wait:
                    await browserAdapter.WaitForSelector((string)pageAction.Value, TimeSpan.FromSeconds(2));
                    break;
                case SerialisablePageActionType.Click:
                    await browserAdapter.Click((string)pageAction.Value);
                    break;
                case SerialisablePageActionType.Scroll:
                    await browserAdapter.ScrollY((int)pageAction.Value);
                    break;
                default:
                    throw new NotSupportedException(pageAction.Type.ToString());
            }
        }
    }
}
