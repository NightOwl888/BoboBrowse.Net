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
    /// AND-NOT operator node for SectionSearchQueryPlan
    /// </summary>
    public class AndNotNode : SectionSearchQueryPlan
    {
        private SectionSearchQueryPlan m_positiveNode;
        private SectionSearchQueryPlan m_negativeNode;

        public AndNotNode(SectionSearchQueryPlan positiveNode, SectionSearchQueryPlan negativeNode)
        {
            m_positiveNode = positiveNode;
            m_negativeNode = negativeNode;
        }

        public override int FetchDoc(int targetDoc)
        {
            m_curDoc = m_positiveNode.FetchDoc(targetDoc);
            m_curSec = -1;
            return m_curDoc;
        }

        public override int FetchSec(int targetSec)
        {
            while (m_curSec < SectionSearchQueryPlan.NO_MORE_SECTIONS)
            {
                m_curSec = m_positiveNode.FetchSec(targetSec);
                if (m_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) break;

                targetSec = m_curSec;

                if (m_negativeNode.DocId < m_curDoc)
                {
                    if (m_negativeNode.FetchDoc(m_curDoc) == DocIdSetIterator.NO_MORE_DOCS) break;
                }

                if (m_negativeNode.DocId == m_curDoc &&
                    (m_negativeNode.SecId == SectionSearchQueryPlan.NO_MORE_SECTIONS ||
                     m_negativeNode.FetchSec(targetSec) > m_curSec))
                {
                    break;
                }
            }
            return m_curSec;
        }
    }
}
