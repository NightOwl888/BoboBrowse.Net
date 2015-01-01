namespace BoboBrowse.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;

    public class AtomicLong
    {
        private long _value = 0;

        public AtomicLong()
        {
        }

        public AtomicLong(long value)
        {
            _value = value;
        }

        public long IncrementAndGet()
        {
            return Interlocked.Increment(ref _value);
        }

        public long GetAndAdd(long value)
        {
            return Interlocked.Add(ref _value, value);
        }

        public long Get()
        {
            return Interlocked.Read(ref _value);
        }
    }
}
