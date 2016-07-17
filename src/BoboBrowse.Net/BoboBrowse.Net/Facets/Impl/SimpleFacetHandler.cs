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
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used when there is a discrete set of facet values, for example: color, with values: red, green, blue, white, black. 
    /// Each document can have only 1 value in this field. When being indexed, this field should not be tokenized.
    /// </summary>
    public class SimpleFacetHandler : FacetHandler<IFacetDataCache>, IFacetScoreable
    {
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
        public SimpleFacetHandler(string name, string indexFieldName, ITermListFactory termListFactory, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {
            _indexFieldName = indexFieldName;
            _termListFactory = termListFactory;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and a list of facets this one depends on for loading.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public SimpleFacetHandler(string name, ITermListFactory termListFactory, IEnumerable<string> dependsOn)
            : this(name, name, termListFactory, dependsOn)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public SimpleFacetHandler(string name, string indexFieldName, ITermListFactory termListFactory)
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
        public SimpleFacetHandler(string name, ITermListFactory termListFactory)
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

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            IFacetDataCache data = GetFacetData<IFacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new FacetDocComparatorSource(this);
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache != null)
            {
                return new string[] { dataCache.ValArray.Get(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            if (dataCache != null)
            {
                return new object[] { dataCache.ValArray.GetRawValue(dataCache.OrderArray.Get(id)) };
            }
            return new string[0];
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            FacetFilter f = new FacetFilter(this, value);
            AdaptiveFacetFilter af = new AdaptiveFacetFilter(
                new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<IFacetDataCache>, _name, _indexFieldName), 
                f, 
                new string[] { value }, 
                false);
            return af;
        }

        private class SimpleFacetHandlerFacetDataCacheBuilder : IFacetDataCacheBuilder
        {
            private readonly Func<BoboSegmentReader, IFacetDataCache> getFacetData;
            private readonly string _name;
            private readonly string _indexFieldName;

            public SimpleFacetHandlerFacetDataCacheBuilder(Func<BoboSegmentReader, IFacetDataCache> getFacetData, string name, string indexFieldName)
            {
                this.getFacetData = getFacetData;
                this._name = name;
                this._indexFieldName = indexFieldName;
            }

            public virtual IFacetDataCache Build(BoboSegmentReader reader)
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

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            if (vals.Length > 1)
            {
                return EmptyFilter.Instance;
            }
            else
            {
                return BuildRandomAccessFilter(vals[0], prop);
            }
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            RandomAccessFilter filter = null;

            if (vals.Length > 1)
            {
                RandomAccessFilter f = new FacetOrFilter(this, vals, false);
                filter = new AdaptiveFacetFilter(
                    new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<IFacetDataCache>, _name, _indexFieldName),
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
                filter = EmptyFilter.Instance;
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
                return new SimpleGroupByFacetHandlerFacetCountCollectorSource(this.GetFacetData<IFacetDataCache>, _name, sel, ospec);
            }
            else
            {
                return new SimpleFacetHandlerFacetCountCollectorSource(this.GetFacetData<IFacetDataCache>, _name, sel, ospec);
            }
        }

        private class SimpleGroupByFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, IFacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public SimpleGroupByFacetHandlerFacetCountCollectorSource(Func<BoboSegmentReader, IFacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _sel = sel;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetDataCache dataCache = getFacetData(reader);
                return new SimpleGroupByFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        private class SimpleFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, IFacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public SimpleFacetHandlerFacetCountCollectorSource(Func<BoboSegmentReader, IFacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _sel = sel;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                IFacetDataCache dataCache = getFacetData(reader);
                return new SimpleFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            IFacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(_indexFieldName, reader, _termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboSegmentReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            IFacetDataCache dataCache = GetFacetData<IFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new SimpleBoboDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class SimpleFacetCountCollector : DefaultFacetCountCollector
        {
            public SimpleFacetCountCollector(string name, IFacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
            }

            public override void Collect(int docid)
            {
                int index = _array.Get(docid);
                _count.Add(index, _count.Get(index) + 1);
            }

            public override void CollectAll()
            {
                _count = BigIntArray.FromArray(_dataCache.Freqs);
            }
        }

        public sealed class SimpleGroupByFacetCountCollector : GroupByFacetCountCollector
        {
            private int _totalGroups;

            public SimpleGroupByFacetCountCollector(string name, IFacetDataCache dataCache, int docBase, BrowseSelection sel, FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                _totalGroups = 0;
            }

            public override sealed void Collect(int docid)
            {
                int index = _array.Get(docid);
                int newValue = _count.Get(index) + 1;
                _count.Add(index, newValue);
                if (newValue <= 1)
                    ++_totalGroups;
            }

            public override sealed void CollectAll()
            {
                _count = BigIntArray.FromArray(_dataCache.Freqs);
                _totalGroups = -1;
            }

            public override sealed int GetTotalGroups()
            {
                if (_totalGroups >= 0)
                    return _totalGroups;

                // If the user calls collectAll instead of collect, we have to collect all the groups here:
                _totalGroups = 0;
                for (int i = 0; i < _count.Size(); i++)
                {
                    int c = _count.Get(i);
                    if (c > 0)
                        ++_totalGroups;
                }
                return _totalGroups;
            }
        }

        public sealed class SimpleBoboDocScorer : BoboDocScorer
        {
            private readonly IFacetDataCache _dataCache;

            public SimpleBoboDocScorer(IFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
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
