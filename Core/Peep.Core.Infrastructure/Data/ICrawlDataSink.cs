using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Data
{
    public interface ICrawlDataSink<T>
    {
        Task Push(string jobId, T data);
    }
}
