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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Query.Scoring;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using BoboBrowse.Net.Util;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Similar to <see cref="T:SimpleFacetHandler"/>, allows a document to have multiple values. 
    /// When being indexed, this field can be tokenized. Or alternatively, one can index multiple 
    /// values in multiple document fields under the same field name.
    /// </summary>
    public class MultiValueFacetHandler : FacetHandler<MultiValueFacetDataCache>, IFacetScoreable
    {
        protected readonly TermListFactory m_termListFactory;
        protected readonly string m_indexFieldName;

        protected int m_maxItems = BigNestedInt32Array.MAX_ITEMS;
        protected Term m_sizePayloadTerm;
        // protected IEnumerable<string> _depends; // NOT USED

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name,
        /// Lucene.Net index field name, <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, size payload term, 
        /// and list of facet handlers this one depends on.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="sizePayloadTerm"></param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm, ICollection<string> dependsOn)
            : base(name, dependsOn)
        {
            //_depends = dependsOn; // NOT USED
            m_indexFieldName = (!string.IsNullOrEmpty(indexFieldName) ? indexFieldName : name);
            m_termListFactory = termListFactory;
            m_sizePayloadTerm = sizePayloadTerm;
        }

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            MultiValueFacetDataCache data = GetFacetData<MultiValueFacetDataCache>(reader);
	        if (data==null) return 0;
	        return data.GetNumItems(id);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name,
        /// Lucene.Net index field name, <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and size payload term.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="sizePayloadTerm"></param>
        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm)
            : this(name, indexFieldName, termListFactory, sizePayloadTerm, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name,
        /// <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance, and size payload term.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        /// <param name="sizePayloadTerm"></param>
        public MultiValueFacetHandler(string name, TermListFactory termListFactory, Term sizePayloadTerm)
            : this(name, name, termListFactory, sizePayloadTerm, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name,
        /// Lucene.Net index field name, and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : this(name, indexFieldName, termListFactory, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name
        /// and <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="termListFactory">A <see cref="T:BoboBrowse.Net.Facets.Data.TermListFactory"/> instance that will create a 
        /// specialized <see cref="T:BoboBrowse.Net.Facets.Data.ITermValueList"/> to compare the field values, typically using their native or primitive data type.</param>
        public MultiValueFacetHandler(string name, TermListFactory termListFactory)
            : this(name, name, termListFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name
        /// and Lucene.Net index field name.
        /// </summary>
        /// <param name="name">The facet handler name.</param>
        /// <param name="indexFieldName">The name of the Lucene.Net index field this handler will utilize.</param>
        public MultiValueFacetHandler(string name, string indexFieldName)
            : this(name, indexFieldName, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name.
        /// The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        public MultiValueFacetHandler(string name)
            : this(name, name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:MultiValueFacetHandler"/> with the specified name
        /// and list of facet handlers this one depends on. The Lucene.Net index field must have the same name.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public MultiValueFacetHandler(string name, ICollection<string> dependsOn)
            : this(name, name, null, null, dependsOn)
        {
        }

        public override DocComparerSource GetDocComparerSource()
        {
            return new MultiFacetDocComparerSource(new MultiDataCacheBuilder(Name, m_indexFieldName));
        }

        public virtual int MaxItems
        {
            set { m_maxItems = Math.Min(value, BigNestedInt32Array.MAX_ITEMS); }
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
            if (dataCache != null)
            {
                return dataCache.NestedArray.GetRawData(id, dataCache.ValArray);
            }
            return new string[0];
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
            return new MultiValueFacetCountCollectorSource(this, m_name, sel, ospec);
        }

        private class MultiValueFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly MultiValueFacetHandler m_parent;
            private readonly string m_name;
            private readonly BrowseSelection m_sel;
            private readonly FacetSpec m_ospec;

            public MultiValueFacetCountCollectorSource(MultiValueFacetHandler parent, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this.m_parent = parent;
                this.m_name = name;
                this.m_sel = sel;
                this.m_ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                MultiValueFacetDataCache dataCache = m_parent.GetFacetData<MultiValueFacetDataCache>(reader);
                return new MultiValueFacetCountCollector(m_name, dataCache, docBase, m_sel, m_ospec);
            }
        }

        public override MultiValueFacetDataCache Load(BoboSegmentReader reader)
        {
            return Load(reader, new BoboSegmentReader.WorkArea());
        }

        public override MultiValueFacetDataCache Load(BoboSegmentReader reader, BoboSegmentReader.WorkArea workArea)
        {
            var dataCache = new MultiValueFacetDataCache();

            dataCache.MaxItems = m_maxItems;

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

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            MultiValueFacetFilter f = new MultiValueFacetFilter(new MultiDataCacheBuilder(Name, m_indexFieldName), value);
            AdaptiveFacetFilter af = new AdaptiveFacetFilter(new SimpleDataCacheBuilder(Name, m_indexFieldName), f, new string[] { value }, false);
            return af;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {

            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    return EmptyFilter.Instance;
                }
            }
            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            RandomAccessFilter filter = null;
            if (vals.Length > 1)
            {
                MultiValueORFacetFilter f = new MultiValueORFacetFilter(this, vals, false);			// catch the "not" case later
                if (!isNot)
                {
                    AdaptiveFacetFilter af = new AdaptiveFacetFilter(new SimpleDataCacheBuilder(Name, m_indexFieldName), f, vals, false);
                    return af;
                }
                else
                {
                    filter = f;
                }
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

        public virtual BoboDocScorer GetDocScorer(BoboSegmentReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new MultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class MultiValueDocScorer : BoboDocScorer
        {
            private readonly MultiValueFacetDataCache m_dataCache;
            private readonly BigNestedInt32Array m_array;

            public MultiValueDocScorer(MultiValueFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.NestedArray.Length), boostList)
            {
                m_dataCache = dataCache;
                m_array = m_dataCache.NestedArray;
            }

            public override Explanation Explain(int doc)
            {
                string[] vals = m_array.GetTranslatedData(doc, m_dataCache.ValArray);

                List<float> scoreList = new List<float>(m_dataCache.ValArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                foreach (string val in vals)
                {
                    int idx = m_dataCache.ValArray.IndexOf(val);
                    if (idx >= 0)
                    {
                        scoreList.Add(m_function.Score(m_dataCache.Freqs[idx], m_boostList[idx]));
                        explList.Add(m_function.Explain(m_dataCache.Freqs[idx], m_boostList[idx]));
                    }
                }
                Explanation topLevel = m_function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                return m_array.GetScores(docid, m_dataCache.Freqs, m_boostList, m_function);
            }
        }

        public sealed class MultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            new private readonly BigNestedInt32Array m_array;

            public MultiValueFacetCountCollector(string name, 
                MultiValueFacetDataCache dataCache, 
                int docBase, 
                BrowseSelection sel, 
                FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                m_array = dataCache.NestedArray;
            }

            public override sealed void Collect(int docid)
            {
                m_array.CountNoReturn(docid, m_count);
            }

            public override sealed void CollectAll()
            {
                m_count = BigInt32Array.FromArray(m_dataCache.Freqs);
            }
        }
    }
}
