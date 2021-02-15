using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Crawler.Infrastructure
{
    public interface IGenericRepository<T>
    {
        Task Set(string id, T data);
        Task<T> Get(string id);
    }
}
