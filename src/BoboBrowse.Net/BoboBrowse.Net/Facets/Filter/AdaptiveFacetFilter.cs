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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets.Data;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using LuceneExt.Impl;
    using System.Collections.Generic;
    using System.Linq;

    public interface IFacetDataCacheBuilder
    {
        FacetDataCache Build(BoboIndexReader reader);
        string Name { get; }
        string IndexFieldName { get; }
    }

    public class AdaptiveFacetFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        private static ILog logger = LogManager.GetLogger(typeof(AdaptiveFacetFilter));

        private readonly RandomAccessFilter _facetFilter;
	    private readonly IFacetDataCacheBuilder _facetDataCacheBuilder;
        private readonly IEnumerable<string> _valSet;
	    private bool  _takeComplement = false;

        /// <summary>
        /// If takeComplement is true, we still return the filter for NotValues.
        /// Therefore, the calling function of this class needs to apply NotFilter on top
        /// of this filter if takeComplement is true.
        /// </summary>
        /// <param name="facetDataCacheBuilder"></param>
        /// <param name="facetFilter"></param>
        /// <param name="val"></param>
        /// <param name="takeComplement"></param>
        public AdaptiveFacetFilter(IFacetDataCacheBuilder facetDataCacheBuilder, RandomAccessFilter facetFilter, string[] val, bool takeComplement)
        {
            _facetFilter = facetFilter;
            _facetDataCacheBuilder = facetDataCacheBuilder;
            _valSet = val;
            _takeComplement = takeComplement;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = _facetFilter.GetFacetSelectivity(reader);
            if (_takeComplement)
                return 1.0 - selectivity;
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            RandomAccessDocIdSet innerDocSet = _facetFilter.GetRandomAccessDocIdSet(reader);
            if (innerDocSet == EmptyDocIdSet.Instance)
            {
                return innerDocSet;
            }

            FacetDataCache dataCache = _facetDataCacheBuilder.Build(reader);
            int totalCount = reader.MaxDoc;
            ITermValueList valArray = dataCache.ValArray;
            int freqCount = 0;

            var validVals = new List<string>(_valSet.Count());
            foreach (string val in _valSet)
            {
                int idx = valArray.IndexOf(val);
                if (idx >= 0)
                {
                    validVals.Add(valArray.Get(idx));  // get and format the value
                    freqCount += dataCache.Freqs[idx];
                }
            }

            if (validVals.Count == 0)
            {
                return EmptyDocIdSet.Instance;
            }

            // takeComplement is only used to choose between TermListRandomAccessDocIdSet and innerDocSet
            int validFreqCount = _takeComplement ? (totalCount - freqCount) : freqCount;

            if (_facetDataCacheBuilder.IndexFieldName != null && ((validFreqCount << 1) < totalCount))
            {
                return new TermListRandomAccessDocIdSet(_facetDataCacheBuilder.IndexFieldName, innerDocSet, validVals, reader);
            }
            else
            {
                return innerDocSet;
            }
        }

        public class TermListRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly RandomAccessDocIdSet _innerSet;
		    private readonly IEnumerable<string> _vals;
		    private readonly IndexReader _reader;
		    private readonly string _name;
            private const int OR_THRESHOLD = 5;

            internal TermListRandomAccessDocIdSet(string name, RandomAccessDocIdSet innerSet, IEnumerable<string> vals, IndexReader reader)
            {
                _name = name;
                _innerSet = innerSet;
                _vals = vals;
                _reader = reader;
            }

            public class TermDocIdSet : DocIdSet
            {
                private readonly Term term;
                private readonly IndexReader reader;

                public TermDocIdSet(IndexReader reader, string name, string val)
                {
                    this.reader = reader;
                    term = new Term(name, val);
                }

                public override DocIdSetIterator Iterator()
                {
                    TermDocs td = reader.TermDocs(term);
                    if (td == null)
                    {
                        return EmptyDocIdSet.Instance.Iterator();
                    }
                    return new TermDocIdSetIterator(td);
                }

                private class TermDocIdSetIterator : DocIdSetIterator
                {
                    private int _doc = -1;
                    private readonly TermDocs _td;

                    public TermDocIdSetIterator(TermDocs td)
                    {
                        _td = td;
                    }

                    public override int Advance(int target)
                    {
                        if (_td.SkipTo(target))
                        {
                            _doc = _td.Doc;
                        }
                        else
                        {
                            _td.Dispose();
                            _doc = DocIdSetIterator.NO_MORE_DOCS;
                        }
                        return _doc;
                    }

                    public override int DocID()
                    {
                        return _doc;
                    }

                    public override int NextDoc()
                    {
                        if (_td.Next())
                        {
                            _doc = _td.Doc;
                        }
                        else
                        {
                            _td.Dispose();
                            _doc = DocIdSetIterator.NO_MORE_DOCS;
                        }
                        return _doc;
                    }
                }
            }

            public override bool Get(int docId)
            {
                return _innerSet.Get(docId);
            }

            public override DocIdSetIterator Iterator()
            {
                if (_vals.Count() == 0)
                {
                    return EmptyDocIdSet.Instance.Iterator();
                }
                if (_vals.Count() == 1)
                {
                    return new TermDocIdSet(_reader, _name, _vals.ElementAt(0)).Iterator();
                }
                else
                {
                    if (_vals.Count() < OR_THRESHOLD)
                    {
                        List<DocIdSet> docSetList = new List<DocIdSet>(_vals.Count());
                        foreach (string val in _vals)
                        {
                            docSetList.Add(new TermDocIdSet(_reader, _name, val));
                        }
                        return new OrDocIdSet(docSetList).Iterator();
                    }
                    else
                    {
                        return _innerSet.Iterator();
                    }
                }
            }
        }
    }
}
