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

// Version compatibility level: 3.1.0
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
    using System;    
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class RangeFacetHandler : FacetHandler<IFacetDataCache>, IFacetScoreable
    {
        private static ILog logger = LogManager.GetLogger<RangeFacetHandler>();
        protected readonly string _indexFieldName;
        protected readonly TermListFactory _termListFactory;
        protected readonly IEnumerable<string> _predefinedRanges;

        public RangeFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, IEnumerable<string> predefinedRanges)
            : base(name)
        {
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
            _predefinedRanges = predefinedRanges;
        }

        public RangeFacetHandler(string name, TermListFactory termListFactory, IEnumerable<string> predefinedRanges)
            : this(name, name, termListFactory, predefinedRanges)
        {
        }

        public RangeFacetHandler(string name, IEnumerable<string> predefinedRanges)
            : this(name, name, null, predefinedRanges)
        {
        }

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
            IFacetDataCache data = GetFacetData<IFacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache != null)
            {
                return new string[] { dataCache.ValArray.Get(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
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
                IFacetDataCache dataCache = _parent.GetFacetData<IFacetDataCache>(reader);
                return new RangeFacetCountCollector(_name, dataCache, docBase, _ospec, _predefinedRanges);
            }
        }

        public bool HasPredefinedRanges
        {
            get { return (_predefinedRanges != null); }
        }

        public override IFacetDataCache Load(BoboIndexReader reader)
        {
            IFacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(_indexFieldName, reader, _termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboIndexReader reader,
            IFacetTermScoringFunctionFactory scoringFunctionFactory,
            IDictionary<string, float> boostMap)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new RangeBoboDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public class RangeBoboDocScorer : BoboDocScorer
        {
            private readonly IFacetDataCache _dataCache;

            public RangeBoboDocScorer(IFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
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
