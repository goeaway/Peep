using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public interface ICrawlDataSinkManager<T>
    {
        Task<int> GetCount(string jobId);
        Task<T> GetData(string jobId);
        Task Clear(string jobId);
    }
}
