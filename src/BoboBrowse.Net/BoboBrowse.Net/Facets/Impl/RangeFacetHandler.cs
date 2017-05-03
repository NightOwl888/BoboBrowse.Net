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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using System.Collections.Generic;

    /// <summary>
    /// Used to denote a range of facet, e.g. dates, prices etc. Each document can have only 1 value in this field. 
    /// When being indexed, this field should not be tokenized. Furthermore, the values need to be formatted to 
    /// ensure sorting by lexical order is the same as the value order. IMPORTANT: <see cref="T:Lucene.Net.Documents.NumericField"/> 
    /// in the Lucene.Net index is not supported, use <see cref="T:Lucene.Net.Documents.Field"/> with a formatted string instead.
    /// </summary>
    public class RangeFacetHandler : FacetHandler<FacetDataCache>, IFacetScoreable
    {
        protected readonly string _indexFieldName;
        protected readonly TermListFactory _termListFactory;
        protected readonly IEnumerable<string> _predefinedRanges;

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetHandler"/> with the specified name, Lucene.Net index field name,
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and predefined ranges.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="predefinedRanges">A set of range strings denoting the beginning and ending of each range, e.g. "[2010/1/1 TO 2012/12/31], [2013/1/1 TO 2015/12/31]".
        /// Date and numeric types are supported. The range values are sorted in lexicographical order, so if you want them formatted a different way, you should provide them in
        /// a specific order. It is valid for the ranges to overlap.</param>
        public RangeFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, IEnumerable<string> predefinedRanges)
            : base(name)
        {
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
            _predefinedRanges = predefinedRanges;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetHandler"/> with the specified name, 
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and predefined ranges.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="predefinedRanges">A set of range strings denoting the beginning and ending of each range, e.g. "[2010/1/1 TO 2012/12/31], [2013/1/1 TO 2015/12/31]".
        /// Date and numeric types are supported. The range values are sorted in lexicographical order, so if you want them formatted a different way, you should provide them in
        /// a specific order. It is valid for the ranges to overlap.</param>
        public RangeFacetHandler(string name, TermListFactory termListFactory, IEnumerable<string> predefinedRanges)
            : this(name, name, termListFactory, predefinedRanges)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetHandler"/> with the specified name and predefined ranges.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="predefinedRanges">A set of range strings denoting the beginning and ending of each range, e.g. "[2010/1/1 TO 2012/12/31], [2013/1/1 TO 2015/12/31]".
        /// Date and numeric types are supported. The range values are sorted in lexicographical order, so if you want them formatted a different way, you should provide them in
        /// a specific order. It is valid for the ranges to overlap.</param>
        public RangeFacetHandler(string name, IEnumerable<string> predefinedRanges)
            : this(name, name, null, predefinedRanges)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetHandler"/> with the specified name, Lucene.Net index field name,
        /// and a set of predefined range values.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="predefinedRanges">A set of range strings denoting the beginning and ending of each range, e.g. "[2010/1/1 TO 2012/12/31], [2013/1/1 TO 2015/12/31]".
        /// Date and numeric types are supported. The range values are sorted in lexicographical order, so if you want them formatted a different way, you should provide them in
        /// a specific order. It is valid for the ranges to overlap.</param>
        public RangeFacetHandler(string name, string indexFieldName, IEnumerable<string> predefinedRanges)
            : this(name, indexFieldName, null, predefinedRanges)
        {
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new FacetDocComparerSource(this);
        }

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            FacetDataCache data = GetFacetData<FacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache != null)
            {
                return new string[] { dataCache.ValArray.Get(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache != null)
            {
                return new object[] { dataCache.ValArray.GetRawValue(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            return new FacetRangeFilter(this, value);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            if (vals.Length > 1)
            {
                return new BitSetFilter(new ValueConverterBitSetBuilder(FacetRangeFilter.FacetRangeValueConverter.Instance, vals, isNot), new SimpleDataCacheBuilder(Name, _indexFieldName));
            }
            else
            {
                RandomAccessFilter filter = BuildRandomAccessFilter(vals[0], prop);
                if (filter == null)
                    return filter;
                if (isNot)
                {
                    filter = new RandomAccessNotFilter(filter);
                }
                return filter;
            }
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new RangeFacetHandlerFacetCountCollectorSource(this, _name, fspec, _predefinedRanges);
        }

        private class RangeFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly RangeFacetHandler _parent;
            private readonly string _name;
            private readonly FacetSpec _ospec;
            private readonly IEnumerable<string> _predefinedRanges;

            public RangeFacetHandlerFacetCountCollectorSource(RangeFacetHandler parent, string name, FacetSpec ospec, IEnumerable<string> predefinedRanges)
            {
                _parent = parent;
                _name = name;
                _ospec = ospec;
                _predefinedRanges = predefinedRanges;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = _parent.GetFacetData<FacetDataCache>(reader);
                return new RangeFacetCountCollector(_name, dataCache, docBase, _ospec, _predefinedRanges);
            }
        }

        public virtual bool HasPredefinedRanges
        {
            get { return (_predefinedRanges != null); }
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(_indexFieldName, reader, _termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboSegmentReader reader,
            IFacetTermScoringFunctionFactory scoringFunctionFactory,
            IDictionary<string, float> boostMap)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new RangeBoboDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class RangeBoboDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache _dataCache;

            public RangeBoboDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Length), boostList)
            {
                _dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int idx = _dataCache.OrderArray.Get(doc);
                return _function.Explain(_dataCache.Freqs[idx], _boostList[idx]);
            }

            public override sealed float Score(int docid)
            {
                int idx = _dataCache.OrderArray.Get(docid);
                return _function.Score(_dataCache.Freqs[idx], _boostList[idx]);
            }
        }
    }
}
