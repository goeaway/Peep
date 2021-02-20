using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Core.Infrastructure.Queuing
{
    public interface ICrawlQueueManager
    {
        Task Clear();
    }
}
