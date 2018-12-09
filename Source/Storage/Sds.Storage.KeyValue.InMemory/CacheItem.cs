using System;

namespace Sds.Storage.KeyValue.InMemory
{
    public class CacheItem
    {
        public byte[] Value { get; }
        public DateTimeOffset Created { get; } = DateTimeOffset.Now;
        public TimeSpan ExpiresAfter { get; set; } = TimeSpan.MaxValue;

        public CacheItem(byte[] value)
        {
            Value = value;
        }
    }
}
