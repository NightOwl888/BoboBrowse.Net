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

    /// <summary>
    /// OR operator node for SectionSearchQueryPlan
    /// </summary>
    public class OrNode : SectionSearchQueryPlan
    {
        private NodeQueue m_pq;

        protected OrNode() { }

        public OrNode(SectionSearchQueryPlan[] subqueries)
        {
            if (subqueries.Length == 0)
            {
                m_curDoc = DocIdSetIterator.NO_MORE_DOCS;
            }
            else
            {
                m_pq = new NodeQueue(subqueries.Length);
                foreach (SectionSearchQueryPlan q in subqueries)
                {
                    if (q != null) m_pq.Add(q);
                }
                m_curDoc = -1;
            }
        }

        public override int FetchDoc(int targetDoc)
        {
            if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS) return m_curDoc;

            if (targetDoc <= m_curDoc) targetDoc = m_curDoc + 1;

            m_curSec = -1;

            SectionSearchQueryPlan node = (SectionSearchQueryPlan)m_pq.Top;
            while (true)
            {
                if (node.DocId < targetDoc)
                {
                    if (node.FetchDoc(targetDoc) < DocIdSetIterator.NO_MORE_DOCS)
                    {
                        node = (SectionSearchQueryPlan)m_pq.UpdateTop();
                    }
                    else
                    {
                        m_pq.Pop();
                        if (m_pq.Count <= 0)
                        {
                            m_curDoc = DocIdSetIterator.NO_MORE_DOCS;
                            return m_curDoc;
                        }
                        node = (SectionSearchQueryPlan)m_pq.Top;
                    }
                }
                else
                {
                    m_curDoc = node.DocId;
                    return m_curDoc;
                }
            }
        }

        public override int FetchSec(int targetSec)
        {
            if (m_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return m_curSec;

            if (targetSec <= m_curSec) targetSec = m_curSec + 1;

            SectionSearchQueryPlan node = (SectionSearchQueryPlan)m_pq.Top;
            while (true)
            {
                if (node.DocId == m_curDoc && m_curSec < SectionSearchQueryPlan.NO_MORE_SECTIONS)
                {
                    if (node.SecId < targetSec)
                    {
                        node.FetchSec(targetSec);
                        node = (SectionSearchQueryPlan)m_pq.UpdateTop();
                    }
                    else
                    {
                        m_curSec = node.SecId;
                        return m_curSec;
                    }
                }
                else
                {
                    m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                    return m_curSec;
                }
            }
        }
    }
}
