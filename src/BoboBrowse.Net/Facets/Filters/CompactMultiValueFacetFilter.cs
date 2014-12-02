

namespace BoboBrowse.Net.Facets.Filters
{
    using System;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Util;

    public class CompactMultiValueFacetFilter : RandomAccessFilter
    {
        private readonly FacetDataCache dataCache;
        private readonly int bits;
        private readonly int[] index;
        private readonly BigSegmentedArray orderArray;

        public CompactMultiValueFacetFilter(FacetDataCache dataCache, int index)
            : this(dataCache, new int[] { index })
        {
        }

        public CompactMultiValueFacetFilter(FacetDataCache dataCache, int[] index)
        {
            this.dataCache = dataCache;
            orderArray = this.dataCache.orderArray;
            this.index = index;
            bits = 0x0;
            foreach (int i in index)
            {
                bits |= 0x00000001 << (i - 1);
            }
        }

        private sealed class CompactMultiValueFacetDocIdSetIterator : DocIdSetIterator
        {
            private int doc;
            private readonly int bits;
            private readonly int maxID;
            private readonly BigSegmentedArray orderArray;

            public CompactMultiValueFacetDocIdSetIterator(FacetDataCache dataCache, int[] index, int bits)
            {
                this.bits = bits;
                doc = int.MaxValue;
                maxID = -1;
                orderArray = dataCache.orderArray;
                foreach (int i in index)
                {
                    if (doc > dataCache.minIDs[i])
                    {
                        doc = dataCache.minIDs[i];
                    }
                    if (maxID < dataCache.maxIDs[i])
                    {
                        maxID = dataCache.maxIDs[i];
                    }
                }
                doc--;
                if (doc < 0)
                {
                    doc = -1;
                }
            }            

            public override int Advance(int target)
            {
                if (target < doc)
                {
                    target = doc + 1;
                }
                doc = orderArray.FindBits(bits, target, maxID);
                return doc;
            }

            public override int DocID()
            {
                return doc;
            }

            public override int NextDoc()
            {
                doc = orderArray.FindBits(bits, doc + 1, maxID);
                return doc;
            }
        }

        private class CompactMultiValueFacetFilterDocIdSet : RandomAccessDocIdSet
        {
            private readonly CompactMultiValueFacetFilter parent;

            public CompactMultiValueFacetFilterDocIdSet(CompactMultiValueFacetFilter parent)
            {
                this.parent = parent;
            }

            public override DocIdSetIterator Iterator()
            {
                return new CompactMultiValueFacetDocIdSetIterator(parent.dataCache, parent.index, parent.bits);
            }

            public override bool Get(int docId)
            {
                return (parent.orderArray.Get(docId) & parent.bits) != 0x0;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(IndexReader reader)
        {
            if (index.Length == 0)
            {
                return EmptyDocIdSet.GetInstance();
            }
            else
            {
                return new CompactMultiValueFacetFilterDocIdSet(this);
            }
        }
    }
}
