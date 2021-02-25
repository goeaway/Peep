using Peep.BrowserAdapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.PageActions
{
    public class SerialisablePageAction : IPageAction
    {
        public string UriRegex { get; set; }
        public object Value { get; set; }
        public SerialisablePageActionType Type { get; set; }
    }
}
