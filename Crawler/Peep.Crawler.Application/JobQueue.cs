using Peep.Crawler.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Peep.Crawler.Application
{
    public class JobQueue : IJobQueue
    {
        private readonly List<IdentifiableCrawlJob> _internalCollection;
        private readonly object _locker = new object();

        public JobQueue()
        {
            _internalCollection = new List<IdentifiableCrawlJob>();
        }

        public void Enqueue(IdentifiableCrawlJob job)
        {
            if(job == null)
            {
                throw new ArgumentNullException(nameof(job));
            }

            lock(_locker)
            {
                _internalCollection.Add(job);
            }
        }

        public bool TryDequeue(out IdentifiableCrawlJob job)
        {
            lock(_locker)
            {
                if(_internalCollection.Any())
                {
                    job = _internalCollection.First();
                    var result = _internalCollection.Remove(job);

                    if(!result)
                    {
                        job = null;
                    }

                    return result;
                }

                job = null;
                return false;
            }   
        }

        public bool TryRemove(string id)
        {
            if(id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            lock(_locker)
            {
                var found = _internalCollection.SingleOrDefault(j => j.Id == id);

                if(found == null)
                {
                    return false;
                }

                return _internalCollection.Remove(found);
            }
        }
    }
}
