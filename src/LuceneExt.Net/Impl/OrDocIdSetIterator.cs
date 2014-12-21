﻿// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt.Impl
{
    using System;
    using System.Collections.Generic;
    using Lucene.Net.Search;

     public class OrDocIdSetIterator : DocIdSetIterator
    {
        private sealed class Item
        {
            public readonly DocIdSetIterator Iter;
            public int Doc;

            public Item(DocIdSetIterator iter)
            {
                Iter = iter;
                Doc = -1;
            }
        }

        private int _curDoc;
        private readonly Item[] _heap;
        private int _size;

        internal OrDocIdSetIterator(List<DocIdSet> sets) // throws IOException
        {
            _curDoc = -1;
            _heap = new Item[sets.Count];
            _size = 0;
            foreach (DocIdSet set in sets)
            {
                _heap[_size++] = new Item(set.Iterator() == null ? DocIdSet.EMPTY_DOCIDSET.Iterator() : set.Iterator());
            }
            if (_size == 0) _curDoc = DocIdSetIterator.NO_MORE_DOCS;
        }

        public override int DocID()
        {
            return _curDoc;
        }

        public override int NextDoc()
        {
            if (_curDoc == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

            Item top = _heap[0];
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
                    if (_size == 0) return (_curDoc = DocIdSetIterator.NO_MORE_DOCS);
                }
                top = _heap[0];
                int topDoc = top.Doc;
                if (topDoc > _curDoc)
                {
                    return (_curDoc = topDoc);
                }
            }
        }

        public override int Advance(int target) 
        {
            if (_curDoc == DocIdSetIterator.NO_MORE_DOCS) return DocIdSetIterator.NO_MORE_DOCS;

            if (target <= _curDoc) target = _curDoc + 1;

            Item top = _heap[0];
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
                    if (_size == 0) return (_curDoc = DocIdSetIterator.NO_MORE_DOCS);
                }
                top = _heap[0];
                int topDoc = top.Doc;
                if (topDoc >= target)
                {
                    return (_curDoc = topDoc);
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
        //  
        //   The subtree of subScorers at root is a min heap except possibly for its root element.
        //   * Bubble the root down as required to make the subtree a heap.
        private void HeapAdjust()
        {
            Item[] heap = this._heap;
            Item top = heap[0];
            int doc = top.Doc;
            int size = this._size;
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
            _size--;
            if (_size > 0)
            {
                Item tmp = _heap[0];
                _heap[0] = _heap[_size];
                _heap[_size] = tmp; // keep the finished iterator at the end for debugging
                HeapAdjust();
            }
        }
    }
}
