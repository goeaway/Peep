using DotNetCore.CAP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Peep.Core.Infrastructure.Subscriptions
{
    public interface ISubscriber<T> : ICapSubscribe
    {
        void CheckMessageReceived(T data);
    }
}
