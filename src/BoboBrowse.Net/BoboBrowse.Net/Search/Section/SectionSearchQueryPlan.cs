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
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public abstract class SectionSearchQueryPlan
    {
        public const int NO_MORE_POSITIONS = int.MaxValue;
        public const int NO_MORE_SECTIONS = int.MaxValue;

        protected int m_curDoc;
        protected int m_curSec;

        /// <summary>
        /// Priority queue of Nodes.
        /// </summary>
        public class NodeQueue : PriorityQueue<SectionSearchQueryPlan>
        {
            public NodeQueue(int size)
                : base(size)
            {
            }

            protected override bool LessThan(SectionSearchQueryPlan nodeA, SectionSearchQueryPlan nodeB)
            {
                if (nodeA.m_curDoc == nodeB.m_curDoc)
                {
                    return (nodeA.m_curSec < nodeB.m_curSec);
                }
                return (nodeA.m_curDoc < nodeB.m_curDoc);
            }
        }

        public SectionSearchQueryPlan()
        {
            m_curDoc = -1;
            m_curSec = -1;
        }

        public virtual int DocId
        {
            get { return m_curDoc; }
        }

        public virtual int SecId
        {
            get { return m_curSec; }
        }

        public virtual int Fetch(int targetDoc)
        {
            while (FetchDoc(targetDoc) < DocIdSetIterator.NO_MORE_DOCS)
            {
                if (FetchSec(0) < SectionSearchQueryPlan.NO_MORE_SECTIONS) return m_curDoc;
            }
            return m_curDoc;
        }

        public abstract int FetchDoc(int targetDoc);

        public abstract int FetchSec(int targetSec);

        protected virtual int FetchPos()
        {
            return NO_MORE_POSITIONS;
        }
    }
}
