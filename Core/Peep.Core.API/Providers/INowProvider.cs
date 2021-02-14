using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.API.Providers
{
    public interface INowProvider
    {
        DateTime Now { get; }
    }
}
