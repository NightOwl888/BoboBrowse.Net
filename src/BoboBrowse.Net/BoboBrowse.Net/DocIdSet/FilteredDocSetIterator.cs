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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.DocIdSet
{
    using Lucene.Net.Search;
    using System;

    public abstract class FilteredDocSetIterator : DocIdSetIterator
    {
        protected DocIdSetIterator _innerIter;
        private int _currentDoc;

        protected FilteredDocSetIterator(DocIdSetIterator innerIter)
        {
            if (innerIter == null)
            {
                throw new ArgumentNullException("null iterator");
            }
            _innerIter = innerIter;
            _currentDoc = -1;
        }

        protected abstract bool Match(int doc);

        public sealed override int DocID()
        {
            return _currentDoc;
        }

        public sealed override int NextDoc()
        {
            int docid = _innerIter.NextDoc();
            while (docid != DocIdSetIterator.NO_MORE_DOCS)
            {
                if (Match(docid))
                {
                    _currentDoc = docid;
                    return docid;
                }
                else
                {
                    docid = _innerIter.NextDoc();
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public sealed override int Advance(int target)
        {
            int docid = _innerIter.Advance(target);
            while (docid != DocIdSetIterator.NO_MORE_DOCS)
            {
                if (Match(docid))
                {
                    _currentDoc = docid;
                    return docid;
                }
                else
                {
                    docid = _innerIter.NextDoc();
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }
    }
}
