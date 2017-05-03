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
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Impl;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MultiRangeFacetHandler : RangeFacetHandler
    {
        private readonly Term m_sizePayloadTerm;
        private int maxItems = BigNestedIntArray.MAX_ITEMS;

        public MultiRangeFacetHandler(string name, string indexFieldName, Term sizePayloadTerm,
            TermListFactory termListFactory, IList<string> predefinedRanges)
            : base(name, indexFieldName, termListFactory, predefinedRanges)
        {
            this.m_sizePayloadTerm = sizePayloadTerm;
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new MultiFacetDocComparerSource(new MultiDataCacheBuilder(Name, m_indexFieldName));
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            if (dataCache != null)
            {
                return dataCache.NestedArray.GetTranslatedData(id, dataCache.ValArray);
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            if (dataCache != null) {
                return dataCache.NestedArray.GetRawData(id, dataCache.ValArray);
            }
            return new string[0];
        }

        public override T GetFacetData<T>(BoboSegmentReader reader)
        {
            return (T)reader.GetFacetData(m_name);
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
 	         return new FacetRangeFilter(this, value);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
 	         return new MultiRangeFacetCountCollectorSource(this, ospec);
        }

        private class MultiRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly MultiRangeFacetHandler m_parent;
            private readonly FacetSpec m_ospec;

            public MultiRangeFacetCountCollectorSource(MultiRangeFacetHandler parent, FacetSpec ospec)
            {
                this.m_parent = parent;
                this.m_ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                MultiValueFacetDataCache dataCache = m_parent.GetFacetData<MultiValueFacetDataCache>(reader);
                BigNestedIntArray _nestedArray = dataCache.NestedArray;
                return new MultiRangeFacetCountCollector(m_parent.Name, dataCache, docBase, this.m_ospec, m_parent.m_predefinedRanges, _nestedArray);
            }

            public class MultiRangeFacetCountCollector : RangeFacetCountCollector
            {
                private readonly BigNestedIntArray m_nestedArray;

                public MultiRangeFacetCountCollector(string name, MultiValueFacetDataCache dataCache, 
                    int docBase, FacetSpec ospec, IList<string> predefinedRanges, BigNestedIntArray nestedArray)
                    : base(name, dataCache, docBase, ospec, predefinedRanges)
                {
                    m_nestedArray = nestedArray;
                }

                public override void Collect(int docid)
                {
                    m_nestedArray.CountNoReturn(docid, m_count);
                }
            }
        }

        public override BoboDocScorer GetDocScorer(BoboSegmentReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory,
            IDictionary<string, float> boostMap)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new MultiValueFacetHandler.MultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
 	         return Load(reader, new BoboSegmentReader.WorkArea());
        }

        public override FacetDataCache Load(BoboSegmentReader reader, BoboSegmentReader.WorkArea workArea)
        {
            MultiValueFacetDataCache dataCache = new MultiValueFacetDataCache();
            dataCache.MaxItems = maxItems;
            if (m_sizePayloadTerm == null)
            {
                dataCache.Load(m_indexFieldName, reader, m_termListFactory, workArea);
            }
            else
            {
                dataCache.Load(m_indexFieldName, reader, m_termListFactory, m_sizePayloadTerm);
            }
            return dataCache;
        }

        public virtual int MaxItems
        {
            set { this.maxItems = value; }
        }
    }
}
