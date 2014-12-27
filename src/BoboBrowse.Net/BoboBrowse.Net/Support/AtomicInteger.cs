namespace BoboBrowse.Net.Support
{
    using System.Threading;

    public class AtomicInteger
    {
        private int _value = 0;

        public AtomicInteger()
        {
        }

        public AtomicInteger(int value)
        {
            _value = value;
        }

        public int IncrementAndGet()
        {
            return Interlocked.Increment(ref _value);
        }
    }
}
