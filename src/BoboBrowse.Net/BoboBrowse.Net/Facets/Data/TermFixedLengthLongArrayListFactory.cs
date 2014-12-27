// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using System;

    public class TermFixedLengthLongArrayListFactory : TermListFactory
    {
        protected int width;

        public TermFixedLengthLongArrayListFactory(int width)
        {
            this.width = width;
        }

        public override ITermValueList CreateTermList()
        {
            return new TermFixedLengthLongArrayList(width);
        }

        public override ITermValueList CreateTermList(int capacity)
        {
            return new TermFixedLengthLongArrayList(width, capacity);
        }
    }
}
