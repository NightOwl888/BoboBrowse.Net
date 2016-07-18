//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.DocIdSet
{
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class AndDocIdSet : ImmutableDocSet
    {
        // private static long serialVersionUID = 1L; // NOT USED
        private readonly IList<int> _interSectionResult = new List<int>();

        [Serializable]
        public class DescDocIdSetComparator : IComparer<StatefulDSIterator>
        {
            // private static long serialVersionUID = 1L; // NOT USED

            public int Compare(StatefulDSIterator o1, StatefulDSIterator o2)
            {
                return o2.DocID() - o1.DocID();
            }
        }

        private IEnumerable<DocIdSet> sets = null;
        private readonly int nonNullSize; // excludes nulls

        public AndDocIdSet(IEnumerable<DocIdSet> docSets)
        {
            this.sets = docSets;
            int size = 0;
            if (sets != null)
            {
                foreach (DocIdSet set in sets)
                {
                    if (set != null) size++;
                }
            }
            nonNullSize = size;
        }

        public IEnumerable<int> GetIntersection()
        {
            return _interSectionResult;
        }

        private class AndDocIdSetIterator : DocIdSetIterator
        {
            int lastReturn = -1;
            private DocIdSetIterator[] iterators = null;

            public AndDocIdSetIterator(int nonNullSize, IEnumerable<DocIdSet> sets)
            {
                if (nonNullSize < 1) throw new ArgumentException("Minimum one iterator required");

                iterators = new DocIdSetIterator[nonNullSize];
                int j = 0;
                foreach (DocIdSet set in sets)
                {
                    if (set != null)
                    {
                        DocIdSetIterator dcit = set.GetIterator();

                        // Note: EMPTY_DOCIDSET has been removed in Lucene 4.8, so using
                        // the built-in EmptyDocIdSet class.
                        //if (dcit == null) dcit = DocIdSet.EMPTY_DOCIDSET.GetIterator();
                        if (dcit == null) dcit = EmptyDocIdSet.Instance.GetIterator();
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
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

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
                            else skip = 0;
                        }
                    }
                    i++;
                }
                return (lastReturn = target);
            }

            public override int Advance(int target)
            {
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

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
                            else skip = 0;
                        }
                    }
                    i++;
                }
                return (lastReturn = target);
            }

            public override long Cost()
            {
                return 0;
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            return new AndDocIdSetIterator(this.nonNullSize, this.sets);
        }

        /// <summary>
        /// Find existence in the set with index
        /// 
        /// NOTE :  Expensive call. Avoid.
        /// </summary>
        /// <param name="val">value to find the index for</param>
        /// <returns>index where the value is</returns>
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new AndDocIdSetIterator(this.nonNullSize, this.sets);
            int cursor = -1;
            try
            {
                int docid;
                while ((docid = finder.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (docid > val) return -1;
                    else if (docid == val) return ++cursor;
                    else ++cursor;

                }
            }
            catch (Exception)
            {
                return -1;
            }
            return -1;
        }

        public override bool Find(int val)
        {
            DocIdSetIterator finder = new AndDocIdSetIterator(this.nonNullSize, this.sets);

            try
            {
                int docid = finder.Advance(val);
                if (docid != DocIdSetIterator.NO_MORE_DOCS && docid == val) return true;
                else return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
