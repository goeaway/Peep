using System;
using System.Collections.Generic;
using System.Linq;
using Peep.Core;

namespace Peep.Crawler.Application.Services
{
    public class JobQueue : IJobQueue
    {
        private readonly List<IdentifiableCrawlJob> _list = new List<IdentifiableCrawlJob>();
        private readonly object _locker = new object();

        public void Enqueue(IdentifiableCrawlJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            lock (_locker)
            {
                _list.Add(job);
            }
        }

        public bool TryRemove(string id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            lock (_locker)
            {
                var found = _list.SingleOrDefault(job => job.Id == id);

                if (found == null)
                {
                    return false;
                }

                return _list.Remove(found);
            }
        }

        public bool TryDequeue(out IdentifiableCrawlJob job)
        {
            lock (_locker)
            {
                job = _list.FirstOrDefault();

                if (job != null)
                {
                    _list.Remove(job);
                }

                return job != null;
            }
        }
    }
}
