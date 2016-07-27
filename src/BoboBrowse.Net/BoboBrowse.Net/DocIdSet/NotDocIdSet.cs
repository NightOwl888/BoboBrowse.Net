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
    using Lucene.Net.Search;
    using System;

    public class NotDocIdSet : ImmutableDocSet
    {
        // private static long serialVersionUID = 1L; // NOT USED

        private DocIdSet innerSet = null;
        private int max = -1;

        public NotDocIdSet(DocIdSet docSet, int maxVal)
        {
            innerSet = docSet;
            max = maxVal;
        }

        public class NotDocIdSetIterator : DocIdSetIterator
        {
            int lastReturn = -1;
            private DocIdSetIterator it1 = null;
            private int innerDocid = -1;
            private readonly DocIdSet innerSet;
            private readonly Func<int> getMax;

            internal NotDocIdSetIterator(DocIdSet innerSet, Func<int> getMax)
            {
                this.innerSet = innerSet;
                this.getMax = getMax;
                Initialize();
            }

            private void Initialize()
            {
                it1 = innerSet.GetIterator();

                try
                {
                    if ((innerDocid = it1.NextDoc()) == DocIdSetIterator.NO_MORE_DOCS) it1 = null;
                }
                catch (Exception)
                {
                    //e.printStackTrace();
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
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                if (target <= lastReturn) target = lastReturn + 1;
                var max = this.getMax();

                if (target >= max)
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
                    if (target >= max)
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

            public override long Cost()
            {
                return 0;
            }
        }

        public override DocIdSetIterator GetIterator()
        {
            return new NotDocIdSetIterator(this.innerSet, () => this.max);
        }

        /// <summary>
        /// Find existence in the set with index
        /// 
        /// NOTE :  Expensive call. Avoid.
        /// </summary>
        /// <param name="target">value to find the index for</param>
        /// <returns>index if the given value</returns>
        public override int FindWithIndex(int target)
        {
            DocIdSetIterator finder = new NotDocIdSetIterator(this.innerSet, () => this.max);
            int cursor = -1;
            try
            {
                int docid;
                while ((docid = finder.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (docid > target) return -1;
                    else if (docid == target) return ++cursor;
                    else ++cursor;

                }
            }
            catch (Exception)
            {
                return -1;
            }
            return -1;
        }
    }
}
