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
    using Lucene.Net.Util;
    using System;

    public class IntMetaDataQuery : MetaDataQuery
    { 
        private Validator m_validator;

        public abstract class Validator
        {
            public abstract bool Validate(int datum);
        }

        public class SimpleValueValidator : Validator
        {
            private readonly int m_val;

            public SimpleValueValidator(int val)
            {
                m_val = val;
            }

            public override bool Validate(int datum)
            {
                return (datum == m_val);
            }

            public override string ToString()
            {
                return "SingleValueValidator[" + m_val + "]";
            }
        }

        public class SimpleRangeValidator : Validator
        {
            private readonly int m_lower;
            private readonly int m_upper;

            public SimpleRangeValidator(int lower, int upper)
            {
                m_lower = lower;
                m_upper = upper;
            }

            public override bool Validate(int datum)
            {
                return (datum >= m_lower && datum <= m_upper);
            }

            public override string ToString()
            {
                return "RangeValidator[" + m_lower + "," + m_upper + "]";
            }
        }

        /// <summary>
        /// constructs IntMetaDataQueryQuery
        /// </summary>
        /// <param name="term"></param>
        /// <param name="validator"></param>
        public IntMetaDataQuery(Term term, Validator validator)
            : base(term)
        {
            m_validator = validator;
        }

        public override string ToString(string field)
        {
            return "IntMetaDataQuery(" + m_validator.ToString() + ")";
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            throw new NotSupportedException();
        }

        public override Query Rewrite(IndexReader reader)
        {
            return this;
        }

        public override SectionSearchQueryPlan GetPlan(AtomicReader reader)
        {
            return new IntMetaDataNodeNoCache(m_term, reader, m_validator);
        }

        public override SectionSearchQueryPlan GetPlan(IMetaDataCache cache)
        {
            return new IntMetaDataNode((IntMetaDataCache)cache, m_validator);
        }

        public class IntMetaDataNodeNoCache : AbstractTerminalNode
        {
            private readonly Validator m_validator;
            private byte[] m_data;
            private int m_dataLen;

            public IntMetaDataNodeNoCache(Term term, AtomicReader reader, Validator validator)
                : base(term, reader)
            {
                m_validator = validator;
            }

            public override int FetchDoc(int targetDoc)
            {
                m_dataLen = -1;
                return base.FetchDoc(targetDoc);
            }

            public override int FetchSec(int targetSec)
            {
                if (m_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return m_curSec;

                if (targetSec <= m_curSec) targetSec = m_curSec + 1;

                if (m_dataLen == -1 && m_posLeft > 0)
                {
                    m_dp.NextPosition();
                    BytesRef payload = m_dp.GetPayload();
                    if (payload != null)
                    {
                        m_dataLen = payload.Length;
                        m_data = payload.Bytes;
                    }
                }
                int offset = targetSec * 4;
                while (offset + 4 <= m_dataLen)
                {
                    int datum = ((m_data[offset] & 0xff) | 
                                ((m_data[offset + 1] & 0xff) << 8) | 
                                ((m_data[offset + 2] & 0xff) << 16) | 
                                ((m_data[offset + 3] & 0xff) << 24));

                    if (m_validator.Validate(datum))
                    {
                        m_curSec = targetSec;
                        return m_curSec;
                    }
                    targetSec++;
                    offset = targetSec * 4;
                }
                m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return m_curSec;
            }
        }

        public class IntMetaDataNode : SectionSearchQueryPlan
        {
            private readonly IntMetaDataCache m_cache;
            private readonly Validator m_validator;
            private readonly int m_maxDoc;

            private int m_maxSec;

            public IntMetaDataNode(IntMetaDataCache cache, Validator validator)
            {
                m_cache = cache;
                m_maxDoc = cache.MaxDoc;
                m_validator = validator;
            }

            public override int FetchDoc(int targetDoc)
            {
                if (m_curDoc == DocIdSetIterator.NO_MORE_DOCS) return m_curDoc;

                if (targetDoc <= m_curDoc) targetDoc = m_curDoc + 1;

                m_curSec = -1;

                while (targetDoc < m_maxDoc)
                {
                    m_maxSec = m_cache.GetNumItems(targetDoc);

                    if (m_maxSec <= 0)
                    {
                        targetDoc++;
                        continue;
                    }
                    m_curDoc = targetDoc;
                    return m_curDoc;
                }
                m_curDoc = DocIdSetIterator.NO_MORE_DOCS;
                return m_curDoc;
            }

            public override int FetchSec(int targetSec)
            {
                if (m_curSec == SectionSearchQueryPlan.NO_MORE_SECTIONS) return m_curSec;

                if (targetSec <= m_curSec) targetSec = m_curSec + 1;

                while (targetSec < m_maxSec)
                {
                    int datum = m_cache.GetValue(m_curDoc, targetSec, 0);

                    if (m_validator.Validate(datum))
                    {
                        m_curSec = targetSec;
                        return m_curSec;
                    }
                    targetSec++;
                }
                m_curSec = SectionSearchQueryPlan.NO_MORE_SECTIONS;
                return m_curSec;
            }
        }
    }
}
