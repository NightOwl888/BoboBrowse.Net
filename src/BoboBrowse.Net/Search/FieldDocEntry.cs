

namespace BoboBrowse.Net.Search
{
    using System;
    using Lucene.Net.Search;

    internal sealed class FieldDocEntry : FieldDoc
    {
        public FieldDocEntry(int slot, int doc, float score)
            : base(doc, score)
        {
            this.Slot = slot;
        }

        public int Slot
        {
            get;
            private set;
        }
    }
}
