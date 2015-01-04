//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
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
    using BoboBrowse.Net.Util;
    using Common.Logging;
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
        private static ILog logger = LogManager.GetLogger<MultiValueFacetHandler>();       

        protected readonly TermListFactory _termListFactory;
        protected readonly string _indexFieldName;

        protected int _maxItems = BigNestedIntArray.MAX_ITEMS;
        protected Term _sizePayloadTerm;
        protected IEnumerable<string> _depends;

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
        public MultiValueFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {
            _depends = dependsOn;
            _indexFieldName = (indexFieldName != null ? indexFieldName : name);
            _termListFactory = termListFactory;
            _sizePayloadTerm = sizePayloadTerm;
        }

        public override int GetNumItems(BoboIndexReader reader, int id)
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
        public MultiValueFacetHandler(string name, IEnumerable<string> dependsOn)
            : this(name, name, null, null, dependsOn)
        {
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new MultiFacetDocComparatorSource(new MultiDataCacheBuilder(Name, _indexFieldName));
        }

        public virtual int MaxItems
        {
            set { _maxItems = Math.Min(value, BigNestedIntArray.MAX_ITEMS); }
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            if (dataCache != null)
            {
                return dataCache.NestedArray.GetTranslatedData(id, dataCache.ValArray);
            }
            return new string[0];
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
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
            return new MultiValueFacetCountCollectorSource(this, _name, sel, ospec);
        }

        public class MultiValueFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly MultiValueFacetHandler _parent;
            private readonly string _name;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;

            public MultiValueFacetCountCollectorSource(MultiValueFacetHandler parent, string name, BrowseSelection sel, FacetSpec ospec)
            {
                this._parent = parent;
                this._name = name;
                this._sel = sel;
                this._ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                MultiValueFacetDataCache dataCache = _parent.GetFacetData<MultiValueFacetDataCache>(reader);
                return new MultiValueFacetCountCollector(_name, dataCache, docBase, _sel, _ospec);
            }
        }

        public override MultiValueFacetDataCache Load(BoboIndexReader reader)
        {
            return Load(reader, new BoboIndexReader.WorkArea());
        }

        public override MultiValueFacetDataCache Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            var dataCache = new MultiValueFacetDataCache();

            dataCache.MaxItems = _maxItems;

            if (_sizePayloadTerm == null)
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, workArea);
            }
            else
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, _sizePayloadTerm);
            }
            return dataCache;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties prop)
        {
            MultiValueFacetFilter f = new MultiValueFacetFilter(new MultiDataCacheBuilder(Name, _indexFieldName), value);
            AdaptiveFacetFilter af = new AdaptiveFacetFilter(new SimpleDataCacheBuilder(Name, _indexFieldName), f, new string[] { value }, false);
            return af;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
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
                    return EmptyFilter.GetInstance();
                }
            }
            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            RandomAccessFilter filter = null;
            if (vals.Length > 1)
            {
                MultiValueORFacetFilter f = new MultiValueORFacetFilter(this, vals, false);			// catch the "not" case later
                if (!isNot)
                {
                    AdaptiveFacetFilter af = new AdaptiveFacetFilter(new SimpleDataCacheBuilder(Name, _indexFieldName), f, vals, false);
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
                filter = EmptyFilter.GetInstance();
            }

            if (isNot)
            {
                filter = new RandomAccessNotFilter(filter);
            }
            return filter;
        }

        public virtual BoboDocScorer GetDocScorer(BoboIndexReader reader, IFacetTermScoringFunctionFactory scoringFunctionFactory, IDictionary<string, float> boostMap)
        {
            MultiValueFacetDataCache dataCache = GetFacetData<MultiValueFacetDataCache>(reader);
            float[] boostList = BoboDocScorer.BuildBoostList(dataCache.ValArray, boostMap);
            return new MultiValueDocScorer(dataCache, scoringFunctionFactory, boostList);
        }

        public sealed class MultiValueDocScorer : BoboDocScorer
        {
            private readonly MultiValueFacetDataCache _dataCache;
            private readonly BigNestedIntArray _array;

            public MultiValueDocScorer(MultiValueFacetDataCache dataCache, IFacetTermScoringFunctionFactory scoreFunctionFactory, float[] boostList)
                : base(scoreFunctionFactory.GetFacetTermScoringFunction(dataCache.ValArray.Count, dataCache.NestedArray.Size), boostList)
            {
                _dataCache = dataCache;
                _array = _dataCache.NestedArray;
            }

            public override Explanation Explain(int doc)
            {
                string[] vals = _array.GetTranslatedData(doc, _dataCache.ValArray);

                List<float> scoreList = new List<float>(_dataCache.ValArray.Count);
                List<Explanation> explList = new List<Explanation>(scoreList.Count);
                foreach (string val in vals)
                {
                    int idx = _dataCache.ValArray.IndexOf(val);
                    if (idx >= 0)
                    {
                        scoreList.Add(_function.Score(_dataCache.Freqs[idx], _boostList[idx]));
                        explList.Add(_function.Explain(_dataCache.Freqs[idx], _boostList[idx]));
                    }
                }
                Explanation topLevel = _function.Explain(scoreList.ToArray());
                foreach (Explanation sub in explList)
                {
                    topLevel.AddDetail(sub);
                }
                return topLevel;
            }

            public override sealed float Score(int docid)
            {
                return _array.GetScores(docid, _dataCache.Freqs, _boostList, _function);
            }
        }

        private sealed class MultiValueFacetCountCollector : DefaultFacetCountCollector
        {
            private readonly new BigNestedIntArray _array;

            public MultiValueFacetCountCollector(string name, 
                MultiValueFacetDataCache dataCache, 
                int docBase, 
                BrowseSelection sel, 
                FacetSpec ospec)
                : base(name, dataCache, docBase, sel, ospec)
            {
                _array = dataCache.NestedArray;
            }

            public override sealed void Collect(int docid)
            {
                _array.CountNoReturn(docid, _count);
            }

            public override sealed void CollectAll()
            {
                _count = _dataCache.Freqs;
            }
        }
    }
}
