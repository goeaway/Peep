using System;

namespace Peep.Core.Infrastructure
{
    public readonly struct CrawlerId
    {
        public string Value { get; }

        public CrawlerId(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value), "value can not be null");
        }

        public override string ToString() => Value;

        public override bool Equals(object obj)
        {
            if (obj is CrawlerId otherCrawlerId)
            {
                return Equals(otherCrawlerId);
            }

            return false;
        }

        public bool Equals(CrawlerId other) => Value == other.Value;
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        public static bool operator ==(CrawlerId a, CrawlerId b) => a.Equals(b);
        public static bool operator !=(CrawlerId a, CrawlerId b) => !(a == b);
        public static implicit operator CrawlerId(string value) => new CrawlerId(value);
        public static CrawlerId FromMachineName() => new CrawlerId(Environment.MachineName);
        public static CrawlerId FromGuid() => new CrawlerId(Guid.NewGuid().ToString());
    }
}