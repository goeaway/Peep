using System.Threading.Tasks;

namespace Peep.Core.Filtering
{
    public interface ICrawlFilter
    {
        int Count { get; }
        Task Add(string uri);
        Task<bool> Contains(string uri);
    }
}
