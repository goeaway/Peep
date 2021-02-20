using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public interface ICrawlDataSink
    {
        Task Push(string jobId, IDictionary<Uri, IEnumerable<string>> data);
    }
}
