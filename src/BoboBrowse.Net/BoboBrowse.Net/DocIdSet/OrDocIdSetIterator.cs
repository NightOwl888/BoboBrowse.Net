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
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using System.Collections.Generic;

    public class OrDocIdSetIterator : DocIdSetIterator
    {
        private sealed class Item
        {
            public DocIdSetIterator Iter { get; private set; }
            public int Doc { get; set; }

            public Item(DocIdSetIterator iter)
            {
                Iter = iter;
                Doc = -1;
            }
        }

        private int m_curDoc;
        private readonly Item[] m_heap;
        private int m_size;

        internal OrDocIdSetIterator(List<DocIdSet> sets)
        {
            m_curDoc = -1;
            m_heap = new Item[sets.Count];
            m_size = 0;
            foreach (DocIdSet set in sets)
            {
                // Note: EMPTY_DOCIDSET has been removed in Lucene 4.8, so using
                // the built-in EmptyDocIdSet class.

                //_heap[_size++] = new Item(set.GetIterator() == null ? DocIdSet.EMPTY_DOCIDSET.GetIterator() : set.GetIterator());
                m_heap[m_size++] = new Item(set.GetIterator() == null ? EmptyDocIdSet.Instance.GetIterator() : set.GetIterator());
                
            }
            if (m_size == 0) m_curDoc = DocIdSetIterator.NO_MORE_DOCS;
        }

        public override int DocID
        {
            get { return m_curDoc; }
        }

        public override int NextDoc()
        {
            if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

            Item top = m_heap[0];
            while (true)
            {
                DocIdSetIterator topIter = top.Iter;
                int docid;
                if ((docid = topIter.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    top.Doc = docid;
                    HeapAdjust();
                }
                else
                {
                    HeapRemoveRoot();
                    if (m_size == 0) return (m_curDoc = DocIdSetIterator.NO_MORE_DOCS);
                }
                top = m_heap[0];
                int topDoc = top.Doc;
                if (topDoc > m_curDoc)
                {
                    return (m_curDoc = topDoc);
                }
            }
        }

        public override int Advance(int target)
        {
            if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

            if (target <= m_curDoc) target = m_curDoc + 1;

            Item top = m_heap[0];
            while (true)
            {
                DocIdSetIterator topIter = top.Iter;
                int docid;
                if ((docid = topIter.Advance(target)) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    top.Doc = docid;
                    HeapAdjust();
                }
                else
                {
                    HeapRemoveRoot();
                    if (m_size == 0) return (m_curDoc = DocIdSetIterator.NO_MORE_DOCS);
                }
                top = m_heap[0];
                int topDoc = top.Doc;
                if (topDoc >= target)
                {
                    return (m_curDoc = topDoc);
                }
            }
        }

        // Organize subScorers into a min heap with scorers generating the earlest document on top.
        //  
        //  private final void heapify() {
        //      int size = _size;
        //      for (int i=(size>>1)-1; i>=0; i--)
        //          heapAdjust(i);
        //  }
         

        /// <summary>
        /// The subtree of subScorers at root is a min heap except possibly for its root element.
        /// Bubble the root down as required to make the subtree a heap.
        /// </summary>
        private void HeapAdjust()
        {
            Item[] heap = this.m_heap;
            Item top = heap[0];
            int doc = top.Doc;
            int size = this.m_size;
            int i = 0;

            while (true)
            {
                int lchild = (i << 1) + 1;
                if (lchild >= size) break;

                Item left = heap[lchild];
                int ldoc = left.Doc;

                int rchild = lchild + 1;
                if (rchild < size)
                {
                    Item right = heap[rchild];
                    int rdoc = right.Doc;

                    if (rdoc <= ldoc)
                    {
                        if (doc <= rdoc) break;

                        heap[i] = right;
                        i = rchild;
                        continue;
                    }
                }

                if (doc <= ldoc) break;

                heap[i] = left;
                i = lchild;
            }
            heap[i] = top;
        }

        // Remove the root Scorer from subScorers and re-establish it as a heap
        private void HeapRemoveRoot()
        {
            m_size--;
            if (m_size > 0)
            {
                Item tmp = m_heap[0];
                m_heap[0] = m_heap[m_size];
                m_heap[m_size] = tmp; // keep the finished iterator at the end for debugging
                HeapAdjust();
            }
        }

        public override long GetCost()
        {
            return 0;
        }
    }
}
