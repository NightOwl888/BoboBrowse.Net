//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    public sealed class FacetRangeFilter : RandomAccessFilter
    {
        private readonly IFacetHandler m_facetHandler;
        private readonly string m_rangeString;

        public FacetRangeFilter(IFacetHandler facetHandler, string rangeString)
        {
            m_facetHandler = facetHandler;
            m_rangeString = rangeString;
        }

        public override double GetFacetSelectivity(BoboSegmentReader reader)
        {
            double selectivity = 0;
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);
            int[] range = Parse(dataCache, m_rangeString);
            if (range != null)
            {
                int accumFreq = 0;
                for (int idx = range[0]; idx <= range[1]; ++idx)
                {
                    accumFreq += dataCache.Freqs[idx];
                }
                int total = reader.MaxDoc;
                selectivity = (double)accumFreq / (double)total;
            }
            if (selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }


        private sealed class FacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int m_doc = -1;

            private int m_minID = int.MaxValue;
            private int m_maxID = -1;
            private readonly int m_start;
            private readonly int m_end;
            private readonly BigSegmentedArray m_orderArray;

            internal FacetRangeDocIdSetIterator(int start, int end, FacetDataCache dataCache)
            {
                m_start = start;
                m_end = end;
                for (int i = start; i <= end; ++i)
                {
                    m_minID = Math.Min(m_minID, dataCache.MinIDs[i]);
                    m_maxID = Math.Max(m_maxID, dataCache.MaxIDs[i]);
                }
                m_doc = Math.Max(-1, m_minID - 1);
                m_orderArray = dataCache.OrderArray;
            }

            public override int DocID
            {
                get { return m_doc; }
            }

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_orderArray.FindValueRange(m_start, m_end, (m_doc + 1), m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public sealed override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (id <= m_maxID) ? m_orderArray.FindValueRange(m_start, m_end, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        private sealed class MultiFacetRangeDocIdSetIterator : DocIdSetIterator
        {
            private int m_doc = -1;
  
            private int m_minID = int.MaxValue;
            private int m_maxID = -1;
            private readonly int m_start;
            private readonly int m_end;
            private readonly BigNestedIntArray m_nestedArray;

            internal MultiFacetRangeDocIdSetIterator(int start, int end, MultiValueFacetDataCache dataCache)
            {
                m_start = start;
                m_end = end;
                for (int i = start; i <= end; ++i)
                {
                    m_minID = Math.Min(m_minID, dataCache.MinIDs[i]);
                    m_maxID = Math.Max(m_maxID, dataCache.MaxIDs[i]);
                }
                m_doc = Math.Max(-1, m_minID - 1);
                m_nestedArray = dataCache.NestedArray;
            }

            public sealed override int DocID
            {
                get { return m_doc; }
            }

            public override int NextDoc()
            {
                m_doc = (m_doc < m_maxID) ? m_nestedArray.FindValuesInRange(m_start, m_end, (m_doc + 1), m_maxID) : NO_MORE_DOCS;
                return m_doc;
            }

            public override int Advance(int id)
            {
                if (m_doc < id)
                {
                    m_doc = (m_doc <= m_maxID) ? m_nestedArray.FindValuesInRange(m_start, m_end, id, m_maxID) : NO_MORE_DOCS;
                    return m_doc;
                }
                return NextDoc();
            }

            public override long GetCost()
            {
                return 0;
            }
        }

        public class FacetRangeValueConverter : IFacetValueConverter
        {
            private readonly static FacetRangeValueConverter m_instance = new FacetRangeValueConverter();
            private FacetRangeValueConverter()
            { }

            public virtual int[] Convert(FacetDataCache dataCache, string[] vals)
            {
                return ConvertIndexes(dataCache, vals);
            }

            /// <summary>
            /// Added in .NET version as an accessor to the instance static field.
            /// </summary>
            /// <returns></returns>
            public static FacetRangeValueConverter Instance
            {
                get { return m_instance; }
            }
        }

        public static int[] ConvertIndexes(FacetDataCache dataCache, string[] vals)
        {
            List<int> list = new List<int>();
            foreach (string val in vals)
            {
                int[] range = Parse(dataCache, val);
                if (range != null)
                {
                    for (int i = range[0]; i <= range[1]; ++i)
                    {
                        list.Add(i);
                    }
                }
            }
            return list.ToArray();
        }

        public static int[] Parse(FacetDataCache dataCache, string rangeString)
        {
            string[] ranges = GetRangeStrings(rangeString);
            string lower = ranges[0];
            string upper = ranges[1];
            string includeLower = ranges[2];
            string includeUpper = ranges[3];

            bool incLower = true, incUpper = true;

            if ("false".Equals(includeLower))
                incLower = false;
            if ("false".Equals(includeUpper))
                incUpper = false;

            if ("*".Equals(lower))
            {
                lower = null;
            }

            if ("*".Equals(upper))
            {
                upper = null;
            }

            int start, end;
            if (lower == null)
            {
                start = 1;
            }
            else
            {
                start = dataCache.ValArray.IndexOf(lower);
                if (start < 0)
                {
                    start = -(start + 1);
                }
                else
                {
                    //when the lower value is in the list, we need to consider if we want this lower value included or not;
                    if (incLower == false)
                    {
                        start++;
                    }

                }
            }

            if (upper == null)
            {
                end = dataCache.ValArray.Count - 1;
            }
            else
            {
                end = dataCache.ValArray.IndexOf(upper);
                if (end < 0)
                {
                    end = -(end + 1) - 1;
                }
                else
                {
                    //when the lower value is in the list, we need to consider if we want this lower value included or not;
                    if (incUpper == false)
                    {
                        end--;
                    }
                }
            }

            return new int[] { start, end };
        }

        public static string[] GetRangeStrings(string rangeString)
        {
            int index2 = rangeString.IndexOf(" TO ");
            bool incLower = true, incUpper = true;

            if (rangeString.Trim().StartsWith("("))
                incLower = false;

            if (rangeString.Trim().EndsWith(")"))
                incUpper = false;

            int index = -1, index3 = -1;

            if (incLower == true)
                index = rangeString.IndexOf('[');
            else if (incLower == false)
                index = rangeString.IndexOf('(');

            if (incUpper == true)
                index3 = rangeString.IndexOf(']');
            else if (incUpper == false)
                index3 = rangeString.IndexOf(')');

            string lower, upper;
            try
            {
                lower = rangeString.Substring(index + 1, index2 - (index + 1)).Trim();
                upper = rangeString.Substring(index2 + 4, index3 - (index2 + 4)).Trim();

                return new string[] { lower, upper, Convert.ToString(incLower).ToLower(), Convert.ToString(incUpper).ToLower() };
            }
            catch (RuntimeException re)
            {
                throw re;
            }
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = m_facetHandler.GetFacetData<FacetDataCache>(reader);

            bool multi = dataCache is MultiValueFacetDataCache;    
            BigNestedIntArray nestedArray = multi ? ((MultiValueFacetDataCache)dataCache).NestedArray : null;
            int[] range = Parse(dataCache, m_rangeString);
    
            if (range == null) 
                return null;
    
            if (range[0] > range[1])
            {
                return EmptyDocIdSet.Instance;
            }

            int start = range[0];
            int end = range[1];

            return new RangeRandomAccessDocIdSet(start, end, dataCache, nestedArray, multi);
        }

        private class RangeRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly int m_start;
            private readonly int m_end;
            private readonly FacetDataCache m_dataCache;
            private readonly BigNestedIntArray m_nestedArray;
            private readonly bool m_multi;

            public RangeRandomAccessDocIdSet(int start, int end, FacetDataCache dataCache, BigNestedIntArray nestedArray, bool multi)
            {
                m_start = start;
                m_end = end;
                m_dataCache = dataCache;
                m_nestedArray = nestedArray;
                m_multi = multi;
            }

            public override bool Get(int docId)
            {
                if (m_multi)
                {
                    m_nestedArray.ContainsValueInRange(docId, m_start, m_end);
                }
                int index = m_dataCache.OrderArray.Get(docId);
                return index >= m_start && index <= m_end;
            }

            public override DocIdSetIterator GetIterator()
            {
                if (m_multi)
                {
                    return new MultiFacetRangeDocIdSetIterator(m_start, m_end, (MultiValueFacetDataCache)m_dataCache);
                }
                else
                {
                    return new FacetRangeDocIdSetIterator(m_start, m_end, m_dataCache);
                }
            }
        }
    }
}
