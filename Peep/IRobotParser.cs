using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep
{
    public interface IRobotParser
    {
        Task<bool> UriForbidden(Uri uri, string userAgent);
    }
}
