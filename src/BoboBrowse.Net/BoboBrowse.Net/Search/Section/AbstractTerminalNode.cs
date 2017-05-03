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
namespace BoboBrowse.Net.Search.Section
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    /// <summary>
    /// An abstract class for terminal nodes of SectionSearchQueryPlan
    /// </summary>
    public abstract class AbstractTerminalNode : SectionSearchQueryPlan
    {
        protected DocsAndPositionsEnum _dp;
        protected int _posLeft;
        protected int _curPos;

        public AbstractTerminalNode(Term term, AtomicReader reader)
        {
            _dp = reader.GetTermPositionsEnum(term);
            _posLeft = 0;
        }

        public virtual int CurPos
        {
            get { return _curPos; }
        }

        public override int FetchDoc(int targetDoc)
        {
            if (targetDoc <= _curDoc) targetDoc = _curDoc + 1;

            if ((_curDoc = _dp.Advance(targetDoc)) != DocsEnum.NO_MORE_DOCS)
            {
                _posLeft = _dp.Freq;
                _curSec = -1;
                _curPos = -1;
                return _curDoc;
            }
            else
            {
                _curDoc = DocIdSetIterator.NO_MORE_DOCS;
                return _curDoc;
            }
        }

        // NOTE: This is already declared in the base class, no need to 
        // do it again.
        // public abstract int FetchSec(int targetSec);
    }
}
