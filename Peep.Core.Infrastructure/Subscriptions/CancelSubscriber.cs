using DotNetCore.CAP;
using Peep.Core.API.Providers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.Infrastructure.Subscriptions
{
    public class CancelSubscriber : ISubscriber<string>
    {
        public const string Key = "cancel";

        private readonly ICrawlCancellationTokenProvider _cancellationTokenProvider;

        public CancelSubscriber(ICrawlCancellationTokenProvider cancellationTokenProvider)
        {
            _cancellationTokenProvider = cancellationTokenProvider;
        }

        [CapSubscribe(Key)]
        public void CheckMessageReceived(string data)
        {
            throw new NotImplementedException();
        }
    }
}
