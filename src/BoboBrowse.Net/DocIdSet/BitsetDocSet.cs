

namespace BoboBrowse.Net.DocIdSet
{
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BitsetDocSet : DocIdSet
    {
        private readonly BitSet _bs;

        public BitsetDocSet()
        {
            _bs = new BitSet();
        }

        public BitsetDocSet(int nbits)
        {
            _bs = new BitSet(nbits);
        }

        public int Size
        {
            get { return _bs.Cardinality(); }
        }

        public override DocIdSetIterator Iterator()
        {
            return new BitsDocIdSetIterator(_bs);
        }

        public class BitsDocIdSetIterator : DocIdSetIterator
        {
            private readonly BitSet _bs;
            private int _current;

            public BitsDocIdSetIterator(BitSet bs)
            {
                _bs = bs;
                _current = -1;
            }

            public override int DocID()
            {
                return _current;
            }

            public override int NextDoc()
            {
                _current = _bs.NextSetBit(_current + 1);
                if (_current != -1)
                {
                    return _current;
                }

                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int Advance(int target)
            {
                _current = _bs.NextSetBit(target);
                if (_current != -1)
                {
                    return _current;
                }

                return DocIdSetIterator.NO_MORE_DOCS;
            }
        }
    }
}
