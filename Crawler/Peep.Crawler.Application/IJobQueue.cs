using Peep.Crawler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Peep.Crawler.Application
{
    public interface IJobQueue
    {
        /// <summary>
        /// Returns true if a job was found and removed, false it not. sets the specified out parameter if job found
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        bool TryDequeue(out IdentifiableCrawlJob job);
        /// <summary>
        /// Enqueues the provided job, ready for processing.
        /// </summary>
        /// <param name="job"></param>
        void Enqueue(IdentifiableCrawlJob job);
        /// <summary>
        /// Returns true if a job with the specified id was found and removed. False if no job found.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool TryRemove(string id);
    }
}
