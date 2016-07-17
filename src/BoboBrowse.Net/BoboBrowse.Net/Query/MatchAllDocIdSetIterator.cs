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
namespace BoboBrowse.Net.Query
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public class MatchAllDocIdSetIterator : DocIdSetIterator
    {
        private readonly Bits _acceptDocs;
        private readonly int _maxDoc;
        private int _docID;
        public MatchAllDocIdSetIterator(AtomicReader reader, Bits acceptDocs)
        {
            _acceptDocs = acceptDocs;
            _maxDoc = reader.MaxDoc;
            _docID = -1;
        }

        public override int Advance(int target)
        {
            _docID = target;
            while (_docID < _maxDoc)
            {
                if (_acceptDocs == null || _acceptDocs.Get(_docID))
                {
                    return _docID;
                }
                _docID++;
            }
            return NO_MORE_DOCS;
        }

        public override int DocID()
        {
            return _docID;
        }

        public override int NextDoc()
        {
            return Advance(_docID + 1);
        }

        public override long Cost()
        {
            return 0;
        }
    }
}
