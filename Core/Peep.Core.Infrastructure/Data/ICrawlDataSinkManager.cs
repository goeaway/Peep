using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public interface ICrawlDataSinkManager
    {
        Task<int> GetCount(string jobId);
        Task<IDictionary<Uri, IEnumerable<string>>> GetData(string jobId);
        Task Clear(string jobId);
    }
}
