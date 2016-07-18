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

    public abstract class DocComparatorSource
    {
        public virtual bool IsReverse { get; set; }

        public abstract DocComparator GetComparator(AtomicReader reader, int docbase);

        public class IntDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public IntDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Ints values = FieldCache.DEFAULT.GetInts(reader, field, true);
                return new IntDocComparator(values);
            }

            private class IntDocComparator : DocComparator
            {
                private readonly FieldCache.Ints values;

                public IntDocComparator(FieldCache.Ints values)
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

        public class StringValComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public StringValComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                // TODO: Not sure about the 3rd parameter...Bobo only had 2, but there is no overload...
                // Need to check the Lucene source code to find out what the overload that is missing does.
                BinaryDocValues values = FieldCache.DEFAULT.GetTerms(reader, this.field, true);
                return new StringValDocComparator(values);
            }

            private class StringValDocComparator : DocComparator
            {
                private readonly BinaryDocValues values;

                public StringValDocComparator(BinaryDocValues values)
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

        public class StringOrdComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public StringOrdComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                SortedDocValues values = FieldCache.DEFAULT.GetTermsIndex(reader, this.field);
                return new StringOrdDocComparator(values);
            }

            private class StringOrdDocComparator : DocComparator
            {
                private readonly SortedDocValues values;

                public StringOrdDocComparator(SortedDocValues values)
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

        public class ShortDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public ShortDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Shorts values = FieldCache.DEFAULT.GetShorts(reader, this.field, true);
                return new ShortDocComparator(values);
            }

            private class ShortDocComparator : DocComparator
            {
                private readonly FieldCache.Shorts values;

                public ShortDocComparator(FieldCache.Shorts values)
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

        public class LongDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public LongDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Longs values = FieldCache.DEFAULT.GetLongs(reader, this.field, true);
                return new LongDocComparator(values);
            }

            private class LongDocComparator : DocComparator
            {
                private readonly FieldCache.Longs values;

                public LongDocComparator(FieldCache.Longs values)
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

        public class FloatDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public FloatDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Floats values = FieldCache.DEFAULT.GetFloats(reader, this.field, true);
                return new FloatDocComparator(values);
            }

            private class FloatDocComparator : DocComparator
            {
                private readonly FieldCache.Floats values;

                public FloatDocComparator(FieldCache.Floats values)
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

        public class DoubleDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public DoubleDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Doubles values = FieldCache.DEFAULT.GetDoubles(reader, this.field, true);
                return new DoubleDocComparator(values);
            }

            private class DoubleDocComparator : DocComparator
            {
                private readonly FieldCache.Doubles values;

                public DoubleDocComparator(FieldCache.Doubles values)
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

        public class RelevanceDocComparatorSource : DocComparatorSource
        {
            public RelevanceDocComparatorSource()
            {
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                return new RelevanceDocComparator();
            }

            private class RelevanceDocComparator : DocComparator
            {
                public RelevanceDocComparator()
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

        public class DocIdDocComparatorSource : DocComparatorSource
        {
            public DocIdDocComparatorSource()
            {
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                return new DocIdDocComparator(docbase);
            }

            private class DocIdDocComparator : DocComparator
            {
                private readonly int docbase;

                public DocIdDocComparator(int docbase)
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

        public class ByteDocComparatorSource : DocComparatorSource
        {
            private readonly string field;

            public ByteDocComparatorSource(string field)
            {
                this.field = field;
            }

            public override DocComparator GetComparator(AtomicReader reader, int docbase)
            {
                FieldCache.Bytes values = FieldCache.DEFAULT.GetBytes(reader, this.field, true);
                return new ByteDocComparator(values);
            }

            private class ByteDocComparator : DocComparator
            {
                private readonly FieldCache.Bytes values;

                public ByteDocComparator(FieldCache.Bytes values)
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
