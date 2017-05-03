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

        public class IntDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public IntDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Int32s values = FieldCache.DEFAULT.GetInt32s(reader, field, true);
                return new IntDocComparer(values);
            }

            private class IntDocComparer : DocComparer
            {
                private readonly FieldCache.Int32s values;

                public IntDocComparer(FieldCache.Int32s values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (values.Get(doc1.Doc) < values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (values.Get(doc1.Doc) > values.Get(doc2.Doc))
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
                    return (IComparable)values.Get(doc.Doc);
                }
            }
        }

        public class StringValComparerSource : DocComparerSource
        {
            private readonly string field;

            public StringValComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                // NOTE: Original Java source called an overload of Lucene 4.3.0 that had only 2 parameters.
                // The setDocsWithField parameter was not available in that version, but since in other methods
                // of this class the parameter is set to true, we am setting it to true here as well.
                BinaryDocValues values = FieldCache.DEFAULT.GetTerms(reader, this.field, true);
                return new StringValDocComparer(values);
            }

            private class StringValDocComparer : DocComparer
            {
                private readonly BinaryDocValues values;

                public StringValDocComparer(BinaryDocValues values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    BytesRef result1 = new BytesRef();
                    BytesRef result2 = new BytesRef();
                    values.Get(doc1.Doc, result1);
                    values.Get(doc2.Doc, result2);

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
                    values.Get(doc.Doc, result);
                    return (IComparable)result.Utf8ToString();
                }
            }
        }

        public class StringOrdComparerSource : DocComparerSource
        {
            private readonly string field;

            public StringOrdComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                SortedDocValues values = FieldCache.DEFAULT.GetTermsIndex(reader, this.field);
                return new StringOrdDocComparer(values);
            }

            private class StringOrdDocComparer : DocComparer
            {
                private readonly SortedDocValues values;

                public StringOrdDocComparer(SortedDocValues values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return values.GetOrd(doc1.Doc) - values.GetOrd(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    int ord = values.GetOrd(doc.Doc);
                    BytesRef term = new BytesRef();
                    values.LookupOrd(ord, term);
                    return term.Utf8ToString();
                }
            }
        }

        public class ShortDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public ShortDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Int16s values = FieldCache.DEFAULT.GetInt16s(reader, this.field, true);
                return new ShortDocComparer(values);
            }

            private class ShortDocComparer : DocComparer
            {
                private readonly FieldCache.Int16s values;

                public ShortDocComparer(FieldCache.Int16s values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return values.Get(doc1.Doc) - values.Get(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)values.Get(doc.Doc);
                }
            }
        }

        public class LongDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public LongDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Int64s values = FieldCache.DEFAULT.GetInt64s(reader, this.field, true);
                return new LongDocComparer(values);
            }

            private class LongDocComparer : DocComparer
            {
                private readonly FieldCache.Int64s values;

                public LongDocComparer(FieldCache.Int64s values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (values.Get(doc1.Doc) < values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (values.Get(doc1.Doc) > values.Get(doc2.Doc))
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
                    return (IComparable)values.Get(doc.Doc);
                }
            }
        }

        public class FloatDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public FloatDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Singles values = FieldCache.DEFAULT.GetSingles(reader, this.field, true);
                return new FloatDocComparer(values);
            }

            private class FloatDocComparer : DocComparer
            {
                private readonly FieldCache.Singles values;

                public FloatDocComparer(FieldCache.Singles values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (values.Get(doc1.Doc) < values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (values.Get(doc1.Doc) > values.Get(doc2.Doc))
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
                    return (IComparable)values.Get(doc.Doc);
                }
            }
        }

        public class DoubleDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public DoubleDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles(reader, this.field, true);
                return new DoubleDocComparer(values);
            }

            private class DoubleDocComparer : DocComparer
            {
                private readonly FieldCache.Doubles values;

                public DoubleDocComparer(FieldCache.Doubles values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    // cannot return v1-v2 because it could overflow
                    if (values.Get(doc1.Doc) < values.Get(doc2.Doc))
                    {
                        return -1;
                    }
                    else if (values.Get(doc1.Doc) > values.Get(doc2.Doc))
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
                    return (IComparable)values.Get(doc.Doc);
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
                private readonly int docbase;

                public DocIdDocComparer(int docbase)
                {
                    this.docbase = docbase;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return doc1.Doc - doc2.Doc;
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)(doc.Doc + this.docbase);
                }
            }
        }

        public class ByteDocComparerSource : DocComparerSource
        {
            private readonly string field;

            public ByteDocComparerSource(string field)
            {
                this.field = field;
            }

            public override DocComparer GetComparer(AtomicReader reader, int docbase)
            {
                FieldCache.Bytes values = FieldCache.DEFAULT.GetBytes(reader, this.field, true);
                return new ByteDocComparer(values);
            }

            private class ByteDocComparer : DocComparer
            {
                private readonly FieldCache.Bytes values;

                public ByteDocComparer(FieldCache.Bytes values)
                {
                    this.values = values;
                }

                public override int Compare(ScoreDoc doc1, ScoreDoc doc2)
                {
                    return values.Get(doc1.Doc) - values.Get(doc2.Doc);
                }

                public override IComparable Value(ScoreDoc doc)
                {
                    return (IComparable)values.Get(doc.Doc);
                }
            }
        }
    }
}
