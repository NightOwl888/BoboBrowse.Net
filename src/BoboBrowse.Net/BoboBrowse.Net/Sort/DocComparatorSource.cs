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
namespace BoboBrowse.Net.Sort
{
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Globalization;

    public abstract class DocComparerSource
    {
        public virtual bool IsReverse { get; set; }

        public abstract DocComparer GetComparer(AtomicReader reader, int docbase);

        /// <summary>
        /// NOTE: This was IntDocComparatorSource in bobo-browse
        /// </summary>
        public class Int32DocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public Int32DocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Int32s values = FieldCache.DEFAULT.GetInt32s(reader, m_field, true);
                return new Int32DocComparer(values);
            }

            /// <summary>
            /// NOTE: This was IntDocComparator in bobo-browse
            /// </summary>
            private class Int32DocComparer : DocComparer
            {
                private readonly FieldCache.Int32s m_values;

                public Int32DocComparer(FieldCache.Int32s values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (m_values.Get(doc1.Doc) < m_values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (m_values.Get(doc1.Doc) > m_values.Get(doc2.Doc))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }

        public class StringValComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public StringValComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                // NOTE: Original Java source called an overload of Lucene 4.3.0 that had only 2 parameters.
                // The setDocsWithField parameter was not available in that version, but since in other methods
                // of this class the parameter is set to true, we are setting it to true here as well.
                BinaryDocValues values = FieldCache.DEFAULT.GetTerms(reader, this.m_field, true);
                return new StringValDocComparer(values);
            }

            private class StringValDocComparer : DocComparer
            {
                private readonly BinaryDocValues m_values;

                public StringValDocComparer(BinaryDocValues values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    BytesRef result1 = new BytesRef();
                    BytesRef result2 = new BytesRef();
                    m_values.Get(doc1.Doc, result1);
                    m_values.Get(doc2.Doc, result2);

                    if (result1.Length == 0)
                    {
                        if (result2.Length == 0)
                        {
                            return 0;
                        }
                        return -1;
                    }
                    else if (result2.Length == 0)
                    {
                        return 1;
                    }
                    return result1.Utf8ToString().CompareTo(result2.Utf8ToString());
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    BytesRef result = new BytesRef();
                    m_values.Get(doc.Doc, result);
                    return (IComparable)result.Utf8ToString();
                }
            }
        }

        public class StringOrdComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public StringOrdComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                SortedDocValues values = FieldCache.DEFAULT.GetTermsIndex(reader, this.m_field);
                return new StringOrdDocComparer(values);
            }

            private class StringOrdDocComparer : DocComparer
            {
                private readonly SortedDocValues m_values;

                public StringOrdDocComparer(SortedDocValues values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return m_values.GetOrd(doc1.Doc) - m_values.GetOrd(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    int ord = m_values.GetOrd(doc.Doc);
                    BytesRef term = new BytesRef();
                    m_values.LookupOrd(ord, term);
                    return term.Utf8ToString();
                }
            }
        }

        /// <summary>
        /// NOTE: This was ShortDocComparatorSource in bobo-browse
        /// </summary>
        public class Int16DocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public Int16DocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
#pragma warning disable 612,618
                FieldCache.Int16s values = FieldCache.DEFAULT.GetInt16s(reader, this.m_field, true);
#pragma warning restore 612, 618
                return new Int16DocComparer(values);
            }

            /// <summary>
            /// NOTE: This was ShortDocComparator in bobo-browse
            /// </summary>
            private class Int16DocComparer : DocComparer
            {
                private readonly FieldCache.Int16s m_values;

                public Int16DocComparer(FieldCache.Int16s values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return m_values.Get(doc1.Doc) - m_values.Get(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }

        /// <summary>
        /// NOTE: This was LongDocComparatorSource in bobo-browse
        /// </summary>
        public class Int64DocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public Int64DocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Int64s values = FieldCache.DEFAULT.GetInt64s(reader, this.m_field, true);
                return new Int64DocComparer(values);
            }

            /// <summary>
            /// NOTE: This was LongDocComparator in bobo-browse
            /// </summary>
            private class Int64DocComparer : DocComparer
            {
                private readonly FieldCache.Int64s m_values;

                public Int64DocComparer(FieldCache.Int64s values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (m_values.Get(doc1.Doc) < m_values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (m_values.Get(doc1.Doc) > m_values.Get(doc2.Doc))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }

        /// <summary>
        /// NOTE: This was FloatDocComparatorSource in bobo-browse
        /// </summary>
        public class SingleDocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public SingleDocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Singles values = FieldCache.DEFAULT.GetSingles(reader, this.m_field, true);
                return new SingleDocComparer(values);
            }

            /// <summary>
            /// NOTE: This was FloatDocComparator in bobo-browse
            /// </summary>
            private class SingleDocComparer : DocComparer
            {
                private readonly FieldCache.Singles m_values;

                public SingleDocComparer(FieldCache.Singles values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (m_values.Get(doc1.Doc) < m_values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (m_values.Get(doc1.Doc) > m_values.Get(doc2.Doc))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }

        public class DoubleDocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public DoubleDocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles(reader, this.m_field, true);
                return new DoubleDocComparer(values);
            }

            private class DoubleDocComparer : DocComparer
            {
                private readonly FieldCache.Doubles m_values;

                public DoubleDocComparer(FieldCache.Doubles values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (m_values.Get(doc1.Doc) < m_values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (m_values.Get(doc1.Doc) > m_values.Get(doc2.Doc))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }

        public class RelevanceDocComparerSource : DocComparerSource
        {
            public RelevanceDocComparerSource()
            {
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                return new RelevanceDocComparer();
            }

            private class RelevanceDocComparer : DocComparer
            {
                public RelevanceDocComparer()
                {
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (doc1.Score < doc2.Score)
                    {
                        return -1;
                    }
                    else if (doc1.Score > doc2.Score)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)doc.Score;
                }
            }
        }

        public class DocIdDocComparerSource : DocComparerSource
        {
            public DocIdDocComparerSource()
            {
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                return new DocIdDocComparer(docbase);
            }

            private class DocIdDocComparer : DocComparer
            {
                private readonly int m_docbase;

                public DocIdDocComparer(int docbase)
                {
                    this.m_docbase = docbase;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return doc1.Doc - doc2.Doc;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)(doc.Doc + this.m_docbase);
                }
            }
        }

        public class ByteDocComparerSource : DocComparerSource
        {
            private readonly string m_field;

            public ByteDocComparerSource(string field)
            {
                this.m_field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
#pragma warning disable 612, 618
                FieldCache.Bytes values = FieldCache.DEFAULT.GetBytes(reader, this.m_field, true);
#pragma warning restore 612, 618
                return new ByteDocComparer(values);
            }

            private class ByteDocComparer : DocComparer
            {
                private readonly FieldCache.Bytes m_values;

                public ByteDocComparer(FieldCache.Bytes values)
                {
                    this.m_values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return m_values.Get(doc1.Doc) - m_values.Get(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)m_values.Get(doc.Doc);
                }
            }
        }
    }
}
