using System;

namespace Peep.Core.API.Providers
{
    public interface INowProvider
    {
        DateTime Now { get; }
    }
}
