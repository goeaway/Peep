using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.API.Application.Providers
{
    public class NowProvider : INowProvider
    {
        private readonly DateTime? _now;
        public DateTime Now => _now.GetValueOrDefault(DateTime.Now);

        public NowProvider()
        {

        }

        public NowProvider(DateTime now)
        {
            _now = now;
        }
    }
}
