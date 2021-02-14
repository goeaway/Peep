using Peep.BrowserAdapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.PageActions
{
    public interface IPageAction
    {
        /// <summary>
        /// Gets or sets a uri regex pattern, if a uri matches this uri regex, the page action should be performed
        /// </summary>
        string UriRegex { get; set; }
        Task Perform(IBrowserAdapter browserAdapter);
    }
}
