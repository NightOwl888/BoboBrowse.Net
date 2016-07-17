//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class OrDocIdSet : ImmutableDocSet
    {
        private const int INVALID = -1;

        [Serializable]
        public class AescDocIdSetComparator : IComparer<DocIdSetIterator>
        {
            public virtual int Compare(DocIdSetIterator o1, DocIdSetIterator o2)
            {
                return o1.DocID() - o2.DocID();
            }
        }

        List<DocIdSet> sets = null;

        private int _size = INVALID;

        public OrDocIdSet(List<DocIdSet> docSets)
        {
            this.sets = docSets;
            int size = 0;
            if (sets != null)
            {
                foreach (DocIdSet set in sets)
                {
                    // TODO: There appears to be a bug in the original Java source,
                    // as the size++ is missing (and we used it this way in older versions).
                    if (set != null) 
                        size++;
                }
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            return new OrDocIdSetIterator(sets);
        }


        /// <summary>
        /// Find existence in the set with index
        /// 
        /// NOTE :  Expensive call. Avoid. 
        /// </summary>
        /// <param name="val"> value to find the index for </param>
        /// <returns> index where the value is </returns>
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new OrDocIdSetIterator(sets);
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

        public override int Size()
        {
            if (_size == INVALID)
            {
                _size = 0;
                DocIdSetIterator it = this.GetIterator();

                try
                {
                    while (it.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        _size++;
                }
                catch
                {
                    _size = INVALID;
                }
            }
            return _size;
        }
    }
}
