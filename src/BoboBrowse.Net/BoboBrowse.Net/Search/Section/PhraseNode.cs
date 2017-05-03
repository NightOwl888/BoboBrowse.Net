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

    /// <summary>
    /// Phrase operator node for SectionSearchQUeryPlan
    /// </summary>
    public class PhraseNode : AndNode
    {
        private TermNode[] m_termNodes;
        private int m_curPos;

        public PhraseNode(TermNode[] termNodes, IndexReader reader)
            : base(termNodes)
        {
            m_termNodes = termNodes;
        }

        public override int FetchDoc(int targetDoc)
        {
            m_curPos = -1;
            return base.FetchDoc(targetDoc);
        }

        public override int FetchSec(int targetSec)
        {
            TermNode firstNode = m_termNodes[0];

            while (FetchPos() < SectionSearchQueryPlan.NO_MORE_POSITIONS)
            {
                int secId = firstNode.ReadSecId();
                if (secId >= targetSec)
                {
                    targetSec = secId;
                    bool matched = true;
                    for (int i = 1; i < m_termNodes.Length; i++)
                    {
                        matched = (targetSec == m_termNodes[i].ReadSecId());
                        if (!matched) break;
                    }
                    if (matched)
                    {
                        m_curSec = targetSec;
                        return m_curSec;
                    }
                }
            }
            m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
            return m_curSec;
        }

        protected override int FetchPos()
        {
            int targetPhrasePos = m_curPos + 1;

            int i = 0;
            while (i < m_termNodes.Length)
            {
                TermNode node = m_termNodes[i];
                int targetTermPos = (targetPhrasePos + node.PositionInPhrase);
                while (node.CurPos < targetTermPos)
                {
                    if (node.FetchPosInternal() == SectionSearchQueryPlan.NO_MORE_POSITIONS)
                    {
                        m_curPos = SectionSearchQueryPlan.NO_MORE_POSITIONS;
                        return m_curPos;
                    }
                }
                if (node.CurPos == targetTermPos)
                {
                    i++;
                }
                else
                {
                    targetPhrasePos = node.CurPos - i;
                    i = 0;
                }
            }
            m_curPos = targetPhrasePos;
            return m_curPos;
        }
    }
}
