//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Common.Logging;
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
        private static ILog logger = LogManager.GetLogger<RangeFacetHandler>();
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

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new FacetDocComparatorSource(this);
        }

        public override int GetNumItems(BoboIndexReader reader, int id)
        {
            FacetDataCache data = GetFacetData<FacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache != null)
            {
                return new string[] { dataCache.ValArray.Get(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache != null)
            {
                return new object[] { dataCache.ValArray.GetRawValue(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
            return new FacetRangeFilter(this, value);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            if (vals.Length > 1)
            {
                return new BitSetFilter(new ValueConverterBitSetBuilder(FacetRangeFilter.FacetRangeValueConverter.instance, vals, isNot), new SimpleDataCacheBuilder(Name, _indexFieldName));
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

        public class RangeFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
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

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache dataCache = _parent.GetFacetData<FacetDataCache>(reader);
                return new RangeFacetCountCollector(_name, dataCache, docBase, _ospec, _predefinedRanges);
            }
        }

        public bool HasPredefinedRanges
        {
            get { return (_predefinedRanges != null); }
        }

        public override FacetDataCache Load(BoboIndexReader reader)
        {
            FacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(_indexFieldName, reader, _termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboIndexReader reader,
            IFacetTermScoringFunctionFactory scoringFunctionFactory,
            IDictionary<string, float> boostMap)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new RangeBoboDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public class RangeBoboDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache _dataCache;

            public RangeBoboDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Size()), boostList)
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
