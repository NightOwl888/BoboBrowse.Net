// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using System;

    public abstract class TermListFactory
    {
        public abstract ITermValueList CreateTermList(int capacity);
        public abstract ITermValueList CreateTermList();
        public abstract Type Type { get; }

        private class DefaultTermListFactory
            : TermListFactory
        {
            public override ITermValueList CreateTermList(int capacity)
            {
                return new TermStringList(capacity);
            }

            public override ITermValueList CreateTermList()
            {
                return new TermStringList();
            }

            public override Type Type
            {
                get { return typeof(string); }
            }
        }

        public static TermListFactory StringListFactory = new DefaultTermListFactory();
    }
}
