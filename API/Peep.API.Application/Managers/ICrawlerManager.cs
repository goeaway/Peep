using System;
using System.Threading.Tasks;

namespace Peep.API.Application.Managers
{
    public interface ICrawlerManager
    {
        void Start(string crawlerId, string jobId);
        void Finish(string crawlerId, string jobId);
        void Clear(string jobId);
        Task WaitAllFinished(string jobId, TimeSpan timeout);
    }
}