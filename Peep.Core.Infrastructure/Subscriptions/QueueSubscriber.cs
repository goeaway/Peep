using DotNetCore.CAP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.Infrastructure.Subscriptions
{
    public class QueueSubscriber : ISubscriber<IdentifiableCrawlJob>
    {
        public const string Key = "queue";

        [CapSubscribe(Key)]
        public void CheckMessageReceived(IdentifiableCrawlJob data)
        {
            throw new NotImplementedException();
        }
    }
}
