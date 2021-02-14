using Peep.Queueing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Peep.Queueing
{
    public class CrawlQueue : ICrawlQueue
    {
        private readonly ConcurrentQueue<Uri> _queue;

        public CrawlQueue() : this(new List<Uri>())
        {
        }

        public CrawlQueue(IEnumerable<Uri> initial)
        {
            _queue = new ConcurrentQueue<Uri>(initial);
        }

        public Task<Uri> Dequeue()
        {
            var success = _queue.TryDequeue(out var next);
            return Task.FromResult(next);
        }

        public Task Enqueue(Uri uri)
        {
            _queue.Enqueue(uri);

            return Task.CompletedTask;
        }

        public Task Enqueue(IEnumerable<Uri> uris)
        {
            foreach(var uri in uris)
            {
                _queue.Enqueue(uri);
            }

            return Task.CompletedTask;
        }

        public Task Clear()
        {
            _queue.Clear();

            return Task.CompletedTask;
        }
    }
}
