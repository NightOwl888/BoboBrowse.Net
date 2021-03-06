﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Used when there is a discrete set of facet values, for example: color, with values: red, green, blue, white, black. 
    /// Each document can have only 1 value in this field. When being indexed, this field should not be tokenized.
    /// </summary>
    public class SimpleFacetHandler : FacetHandler<FacetDataCache>, IFacetScoreable
    {
        protected TermListFactory m_termListFactory;
        protected readonly string m_indexFieldName;

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and a list of facets this one depends on for loading.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public SimpleFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, ICollection<string> dependsOn)
            : base(name, dependsOn)
        {
            m_indexFieldName = indexFieldName;
            m_termListFactory = termListFactory;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:SimpleFacetHandler"/> with the specified name, Lucene.Net index field name, 
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and a list of facets this one depends on for loading.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public SimpleFacetHandler(string name, TermListFactory termListFactory, ICollection<string> dependsOn)
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

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            FacetDataCache data = GetFacetData<FacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new FacetDocComparerSource(this);
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
            FacetFilter f = new FacetFilter(this, value);
            AdaptiveFacetFilter af = new AdaptiveFacetFilter(
                new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<FacetDataCache>, m_name, m_indexFieldName), 
                f, 
                new string[] { value }, 
                false);
            return af;
        }

        private class SimpleFacetHandlerFacetDataCacheBuilder : IFacetDataCacheBuilder
        {
            private readonly Func<BoboSegmentReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly string _indexFieldName;

            public SimpleFacetHandlerFacetDataCacheBuilder(Func<BoboSegmentReader, FacetDataCache> getFacetData, string name, string indexFieldName)
            {
                this.getFacetData = getFacetData;
                this._name = name;
                this._indexFieldName = indexFieldName;
            }

            public virtual FacetDataCache Build(BoboSegmentReader reader)
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
                    new SimpleFacetHandlerFacetDataCacheBuilder(this.GetFacetData<FacetDataCache>, m_name, m_indexFieldName),
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
                return new SimpleGroupByFacetHandlerFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, m_name, sel, ospec);
            }
            else
            {
                return new SimpleFacetHandlerFacetCountCollectorSource(this.GetFacetData<FacetDataCache>, m_name, sel, ospec);
            }
        }

        private class SimpleGroupByFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, FacetDataCache> getFacetData;
            private readonly string m_name;
            private readonly BrowseSelection m_sel;
            private readonly FacetSpec m_ospec;

            public SimpleGroupByFacetHandlerFacetCountCollectorSource(Func<BoboSegmentReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                m_name = name;
                m_sel = sel;
                m_ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = getFacetData(reader);
                return new SimpleGroupByFacetCountCollector(m_name, dataCache, docBase, m_sel, m_ospec);
            }
        }

        private class SimpleFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly Func<BoboSegmentReader, FacetDataCache> getFacetData;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public SimpleFacetHandlerFacetCountCollectorSource(Func<BoboSegmentReader, FacetDataCache> getFacetData, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.getFacetData = getFacetData;
                _name = name;
                _sel = sel;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = getFacetData(reader);
                return new SimpleFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            FacetDataCache dataCache = new FacetDataCache();
            dataCache.Load(m_indexFieldName, reader, m_termListFactory);
            return dataCache;
        }

        public virtual BoboDocScorer GetDocScorer(BoboSegmentReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
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
                int index = m_array.Get(docid);
                m_count.Add(index, m_count.Get(index) + 1);
            }

            public override void CollectAll()
            {
                m_count = BigInt32Array.FromArray(m_dataCache.Freqs);
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
                int index = m_array.Get(docid);
                int newValue = m_count.Get(index) + 1;
                m_count.Add(index, newValue);
                if (newValue <= 1)
                    ++_totalGroups;
            }

            public override sealed void CollectAll()
            {
                m_count = BigInt32Array.FromArray(m_dataCache.Freqs);
                _totalGroups = -1;
            }

            public override sealed int GetTotalGroups()
            {
                if (_totalGroups >= 0)
                    return _totalGroups;

                // If the user calls collectAll instead of collect, we have to collect all the groups here:
                _totalGroups = 0;
                for (int i = 0; i < m_count.Length; i++)
                {
                    int c = m_count.Get(i);
                    if (c > 0)
                        ++_totalGroups;
                }
                return _totalGroups;
            }
        }

        public sealed class SimpleBoboDocScorer : BoboDocScorer
        {
            private readonly FacetDataCache m_dataCache;

            public SimpleBoboDocScorer(FacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.OrderArray.Length), boostList)
            {
                m_dataCache = dataCache;
            }

            public override Explanation Explain(int doc)
            {
                int idx = m_dataCache.OrderArray.Get(doc);
                return m_function.Explain(m_dataCache.Freqs[idx], m_boostList[idx]);
            }

            public override sealed float Score(int docid)
            {
                int idx = m_dataCache.OrderArray.Get(docid);
                return m_function.Score(m_dataCache.Freqs[idx], m_boostList[idx]);
            }
        }
    }
}
