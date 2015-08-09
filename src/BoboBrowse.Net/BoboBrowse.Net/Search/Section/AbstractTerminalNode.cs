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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    /// <summary>
    /// An abstract class for terminal nodes of SectionSearchQueryPlan
    /// </summary>
    public abstract class AbstractTerminalNode : SectionSearchQueryPlan
    {
        protected TermPositions _tp;
        protected int _posLeft;
        protected int _curPos;

        public AbstractTerminalNode(Term term, IndexReader reader)
        {
            _tp = reader.TermPositions();
            _tp.Seek(term);
            _posLeft = 0;
        }

        public virtual int CurPos
        {
            get { return _curPos; }
        }

        public override int FetchDoc(int targetDoc)
        {
            if (targetDoc <= _curDoc) targetDoc = _curDoc + 1;

            if (_tp.SkipTo(targetDoc))
            {
                _curDoc = _tp.Doc;
                _posLeft = _tp.Freq;
                _curSec = -1;
                _curPos = -1;
                return _curDoc;
            }
            else
            {
                _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                _tp.Dispose();
                return _curDoc;
            }
        }
    }
}
