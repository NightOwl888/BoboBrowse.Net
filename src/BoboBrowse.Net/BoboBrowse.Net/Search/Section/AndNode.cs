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
    /// AND operator node for SectionSearchQueryPlan
    /// </summary>
    public class AndNode : SectionSearchQueryPlan
    {
        protected SectionSearchQueryPlan[] m_subqueries;

        public AndNode(SectionSearchQueryPlan[] subqueries)
        {
            m_subqueries = subqueries;
            m_curDoc = (subqueries.Length > 0 ? -1 : DocIdSetIterator.NO_MORE_DOCS);
        }

        public override int FetchDoc(int targetDoc)
        {
            if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return m_curDoc;
            }

            SectionSearchQueryPlan node = m_subqueries[0];
            m_curDoc = node.FetchDoc(targetDoc);
            targetDoc = m_curDoc;

            int i = 1;
            while (i < m_subqueries.Length)
            {
                node = m_subqueries[i];
                if (node.DocId < targetDoc)
                {
                    m_curDoc = node.FetchDoc(targetDoc);
                    if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        return m_curDoc;
                    }

                    if (m_curDoc > targetDoc)
                    {
                        targetDoc = m_curDoc;
                        i = 0;
                        continue;
                    }
                }
                i++;
            }
            m_curSec = -1;
            return m_curDoc;
        }

        public override int FetchSec(int targetSec)
        {
            SectionSearchQueryPlan node = m_subqueries[0];
            targetSec = node.FetchSec(targetSec);
            if (targetSec == SectionSearchQueryPlan.NO_MORE_SECTIONS)
            {
                m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return targetSec;
            }

            int i = 1;
            while (i < m_subqueries.Length)
            {
                node = m_subqueries[i];
                if (node.SecId < targetSec)
                {
                    m_curSec = node.FetchSec(targetSec);
                    if (m_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS)
                    {
                        return m_curSec;
                    }

                    if (m_curSec > targetSec)
                    {
                        targetSec = m_curSec;
                        i = 0;
                        continue;
                    }
                }
                i++;
            }
            return m_curSec;
        }
    }
}
