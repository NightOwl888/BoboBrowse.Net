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
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Common.Logging;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used when there is a discrete set of facet values, for example: color, with values: red, green, blue, white, black. 
    /// Each document can have only 1 value in this field. When being indexed, this field should not be tokenized.
    /// </summary>
    public class SimpleFacetHandler : FacetHandler<FacetDataCache>, IFacetScoreable
    {
        private static ILog logger = LogManager.GetLogger<SimpleFacetHandler>();
        protected TermListFactory _termListFactory;
        protected readonly string _indexFieldName;

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and a list of facets this one depends on for loading.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public SimpleFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public SimpleFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : this(name, indexFieldName, termListFactory, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public SimpleFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name.
        /// The Lucene.Net index field must have the same name. A <see cref="T:BoboBrowse.Net.Facets.Data.TermStringList"/> will be
        /// used to store the data elements for comparison.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        public SimpleFacetHandler(string name)
            : this(name, name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name
        /// and Lucene.Net index field name. A <see cref="T:BoboBrowse.Net.Facets.Data.TermStringList"/> will be
        /// used to store the data elements for comparison.
        /// </summary>
        /// <param name="name">The name of the facet handler.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        public SimpleFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        public override int GetNumItems(BoboIndexReader reader, int id)
        {
            FacetDataCache data = GetFacetData<FacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new FacetDocComparatorSource(this);
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
            FacetFilter f = new FacetFilter(this, value);
            AdaptiveFacetFilter af = new AdaptiveFacetFilter(
                new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<FacetDataCache>, _name, _indexFieldName), 
                f, 
                new string[] { value }, 
                false);
            return af;
        }

        public class SimpleFacetHandlerFacetDataCacheBuilder : IFacetDataCacheBuilder
        {
            private readonly Func<BoboIndexReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly string _indexFieldName;

            public SimpleFacetHandlerFacetDataCacheBuilder(Func<BoboIndexReader, FacetDataCache> getFacetData, string name, string indexFieldName)
            {
                this.getFacetData = getFacetData;
                this._name = name;
                this._indexFieldName = indexFieldName;
            }

            public virtual FacetDataCache Build(BoboIndexReader reader)
            {
                return getFacetData(reader);
            }

            public virtual string Name
            {
                get { return _name; }
            }

            public virtual string IndexFieldName
            {
                get { return _indexFieldName; }
            }
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            if (vals.Length > 1)
            {
                return EmptyFilter.GetInstance();
            }
            else
            {
                return BuildRandomAccessFilter(vals[0], prop);
            }
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            if (vals.Length > 1)
            {
                RandomAccessFilter f = new FacetOrFilter(this, vals, false);
                filter = new AdaptiveFacetFilter(
                    new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<FacetDataCache>, _name, _indexFieldName),
                    f,
                    vals,
                    isNot);
            }
            else if (vals.Length == 1)
            {
                filter = BuildRandomAccessFilter(vals[0], prop);
            }
            else
            {
                filter = EmptyFilter.GetInstance();
            }

            if (isNot)
            {
                filter = new RandomAccessNotFilter(filter);
            }

            return filter;
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
            return GetFacetCountCollectorSource(sel, ospec, false);
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec, bool groupMode)
        {
            if (groupMode)
            {
                return new SimpleGroupByFacetHandlerFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, _name, sel, ospec);
            }
            else
            {
                return new SimpleFacetHandlerFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, _name, sel, ospec);
            }
        }

        private class SimpleGroupByFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboIndexReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public SimpleGroupByFacetHandlerFacetCountCollectorSource(Func<BoboIndexReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _sel = sel;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache dataCache = getFacetData(reader);
                return new SimpleGroupByFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        private class SimpleFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboIndexReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public SimpleFacetHandlerFacetCountCollectorSource(Func<BoboIndexReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _sel = sel;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache dataCache = getFacetData(reader);
                return new SimpleFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        public override FacetDataCache Load(BoboIndexReader reader)
        {
            FacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(_indexFieldName, reader, _termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboIndexReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new SimpleBoboDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class SimpleFacetCountCollector : DefaultFacetCountCollector
        {
            public SimpleFacetCountCollector(string name, FacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
            }

            public override void Collect(int docid)
            {
                _count[_array.Get(docid)]++;
            }

            public override void CollectAll()
            {
                _count = _dataCache.Freqs;
            }
        }

        public sealed class SimpleGroupByFacetCountCollector : GroupByFacetCountCollector
        {
            private int _totalGroups;

            public SimpleGroupByFacetCountCollector(string name, FacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                _totalGroups = 0;
            }

            public override sealed void Collect(int docid)
            {
                if (++_count[_array.Get(docid)] <= 1)
                    ++_totalGroups;
            }

            public override sealed void CollectAll()
            {
                _count = _dataCache.Freqs;
                _totalGroups = -1;
            }

            public override sealed int GetTotalGroups()
            {
                if (_totalGroups >= 0)
                    return _totalGroups;

                // If the user calls collectAll instead of collect, we have to collect all the groups here:
                _totalGroups = 0;
                foreach (int c in _count)
                {
                    if (c > 0)
                        ++_totalGroups;
                }
                return _totalGroups;
            }
        }

        public sealed class SimpleBoboDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache _dataCache;

            public SimpleBoboDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
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
