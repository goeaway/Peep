using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Providers
{
    public interface INowProvider
    {
        DateTime Now { get; }
    }
}
