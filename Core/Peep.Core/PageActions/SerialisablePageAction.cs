﻿using Peep.Core.BrowserAdapter;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.PageActions
{
    public class SerialisablePageAction : IPageAction
    {
        public string UriRegex { get; set; }
        public object Value { get; set; }
        public SerialisablePageActionType Type { get; set; }

        public async Task Perform(IBrowserAdapter browserAdapter)
        {
            if(browserAdapter == null)
            {
                throw new ArgumentNullException(nameof(browserAdapter));
            }

            switch(Type)
            {
                case SerialisablePageActionType.Wait:
                    await browserAdapter.WaitForSelector((string)Value, TimeSpan.FromSeconds(2));
                    break;
                case SerialisablePageActionType.Click:
                    await browserAdapter.Click((string)Value);
                    break;
                case SerialisablePageActionType.Scroll:
                    await browserAdapter.ScrollY((int)Value);
                    break;
                default:
                    throw new NotSupportedException(Type.ToString());
            }
        }
    }
}