﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.Impl
{
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;    

    [Serializable]
    public class AndDocIdSet : ImmutableDocSet
    {
        private static long serialVersionUID = 1L;
        private List<int> _interSectionResult = new List<int>();

        [Serializable]
        public class DescDocIdSetComparator : IComparer<StatefulDSIterator>
        {
            private static long serialVersionUID = 1L;

            public virtual int Compare(StatefulDSIterator o1, StatefulDSIterator o2)
            {
                return o2.DocID() - o1.DocID();
            }
        }

        private List<DocIdSet> sets = null;
        private int nonNullSize; // excludes nulls

        public AndDocIdSet(List<DocIdSet> docSets)
        {
            this.sets = docSets;
            int size = 0;
            if (sets != null)
            {
                foreach (DocIdSet set in sets)
                {
                    if (set != null)
                        size++;
                }
            }
            nonNullSize = size;
        }

        public virtual IEnumerable<int> Intersection
        {
            get { return _interSectionResult; }
        }

        internal class AndDocIdSetIterator : DocIdSetIterator
        {
            internal int lastReturn = -1;
            private DocIdSetIterator[] iterators = null;

            internal AndDocIdSetIterator(AndDocIdSet parent)
            {
                if (parent.nonNullSize < 1)
                    throw new ArgumentException("Minimum one iterator required");

                iterators = new DocIdSetIterator[parent.nonNullSize];
                int j = 0;
                foreach (DocIdSet set in parent.sets)
                {
                    if (set != null)
                    {
                        DocIdSetIterator dcit = set.Iterator();
                        if (dcit == null)
                            dcit = DocIdSet.EMPTY_DOCIDSET.Iterator();
                        iterators[j++] = dcit;
                    }
                }
                lastReturn = (iterators.Length > 0 ? -1 : DocIdSetIterator.NO_MORE_DOCS);
            }

            public override int DocID()
            {
                return lastReturn;
            }

            public override int NextDoc()
            {

                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                    return DocIdSetIterator.NO_MORE_DOCS;

                DocIdSetIterator dcit = iterators[0];
                int target = dcit.NextDoc();
                int size = iterators.Length;
                int skip = 0;
                int i = 1;
                while (i < size)
                {
                    if (i != skip)
                    {
                        dcit = iterators[i];
                        int docid = dcit.Advance(target);

                        if (docid > target)
                        {
                            target = docid;
                            if (i != 0)
                            {
                                skip = i;
                                i = 0;
                                continue;
                            }
                            else
                                skip = 0;
                        }
                    }
                    i++;
                }
                //      if(target != DocIdSetIterator.NO_MORE_DOCS)
                //        _interSectionResult.Add(target);
                return (lastReturn = target);
            }

            public override int Advance(int target)
            {

                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                    return DocIdSetIterator.NO_MORE_DOCS;

                DocIdSetIterator dcit = iterators[0];
                target = dcit.Advance(target);
                int size = iterators.Length;
                int skip = 0;
                int i = 1;
                while (i < size)
                {
                    if (i != skip)
                    {
                        dcit = iterators[i];
                        int docid = dcit.Advance(target);
                        if (docid > target)
                        {
                            target = docid;
                            if (i != 0)
                            {
                                skip = i;
                                i = 0;
                                continue;
                            }
                            else
                                skip = 0;
                        }
                    }
                    i++;
                }
                return (lastReturn = target);
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new AndDocIdSetIterator(this);
            //return new AndDocIdSetIterator2(sets);
        }

        ///  
        ///<summary>Find existence in the set with index
        ///   * 
        ///   * NOTE :  Expensive call. Avoid. </summary>
        ///   * <param name="val"> value to find the index for </param>
        ///   * <returns> index where the value is </returns>
        ///   
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new AndDocIdSetIterator(this);
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

        public override bool Find(int val)
        {
            DocIdSetIterator finder = new AndDocIdSetIterator(this);

            try
            {
                int docid = finder.Advance(val);
                if (docid != DocIdSetIterator.NO_MORE_DOCS && docid == val)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
