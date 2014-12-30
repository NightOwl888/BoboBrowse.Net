﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.Impl
{
    using System;
    using System.IO;
    using Lucene.Net.Search;


    [Serializable]
    public class NotDocIdSet : ImmutableDocSet
    {       
        private readonly DocIdSet innerSet = null;

        private readonly int max = -1;

        public NotDocIdSet(DocIdSet docSet, int maxVal)
        {
            innerSet = docSet;
            max = maxVal;
        }

        internal class NotDocIdSetIterator : DocIdSetIterator
        {
            internal int lastReturn = -1;
            private DocIdSetIterator it1 = null;
            private int innerDocid = -1;
            private NotDocIdSet parent;

            internal NotDocIdSetIterator(NotDocIdSet parent)
            {
                this.parent = parent;
                Initialize();
            }

            private void Initialize()
            {
                it1 = parent.innerSet.Iterator();

                try
                {
                    if ((innerDocid = it1.NextDoc()) == DocIdSetIterator.NO_MORE_DOCS) it1 = null;
                }
                catch
                {                 
                }
            }

            public override int DocID()
            {
                return lastReturn;
            }

            public override int NextDoc()
            {
                return Advance(0);
            }

            public override int Advance(int target)
            {
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

                if (target <= lastReturn) target = lastReturn + 1;

                if (target >= parent.max)
                {
                    return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
                }

                if (it1 != null && innerDocid < target)
                {
                    if ((innerDocid = it1.Advance(target)) == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        it1 = null;
                    }
                }

                while (it1 != null && innerDocid == target)
                {
                    target++;
                    if (target >= parent.max)
                    {
                        return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
                    }
                    if ((innerDocid = it1.Advance(target)) == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        it1 = null;
                    }
                }
                return (lastReturn = target);
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new NotDocIdSetIterator(this);
        }

        ///  
        ///<summary>Find existence in the set with index
        ///   * 
        ///   * NOTE :  Expensive call. Avoid. </summary>
        ///   * <param name="val"> value to find the index for </param>
        ///   * <returns> index if the given value </returns>
        ///   
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new NotDocIdSetIterator(this);
            int cursor = -1;
            try
            {
                int docid;
                while ((docid = finder.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (docid > val)
                        return -1;
                    else if (docid == val)
                        return ++cursor;
                    else
                        ++cursor;



                }
            }
            catch
            {
                return -1;
            }
            return -1;
        }
    }
}
