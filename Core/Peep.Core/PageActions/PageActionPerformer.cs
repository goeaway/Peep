using System;
using System.Threading.Tasks;
using Peep.BrowserAdapter;

namespace Peep.Core.PageActions
{
    public class PageActionPerformer : IPageActionPerformer
    {
        public async Task Perform(IPageAction pageAction, IPageAdapter pageAdapter)
        {
            if (pageAction == null)
            {
                throw new ArgumentNullException(nameof(pageAction));
            }
            
            if (pageAdapter == null)
            {
                throw new ArgumentNullException(nameof(pageAdapter));
            }

            switch (pageAction.Type)
            {
                case SerialisablePageActionType.Wait:
                    await pageAdapter.WaitForSelector((string)pageAction.Value, TimeSpan.FromSeconds(10));
                    break;
                case SerialisablePageActionType.Click:
                    await pageAdapter.Click((string)pageAction.Value);
                    break;
                case SerialisablePageActionType.Scroll:
                    await pageAdapter.ScrollY((int)pageAction.Value);
                    break;
                default:
                    throw new NotSupportedException(pageAction.Type.ToString());
            }
        }
    }
}
