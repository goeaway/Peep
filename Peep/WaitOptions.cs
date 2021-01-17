using System;
using System.Collections.Generic;
using System.Text;

namespace Peep
{
    public class WaitOptions
    {
        /// <summary>
        /// Gets or sets a period defined in milliseconds that should elapse before we give up waiting for the selector to find an element on a page
        /// </summary>
        public int MillisecondsTimeout { get; set; }
        /// <summary>
        /// Gets or sets the CSS style selector the crawler should wait for before continuing or timing out.
        /// </summary>
        public string Selector { get; set; }
    }
}
