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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Search
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    
    public sealed class FacetHitCollector
    {
        public FacetCountCollectorSource _facetCountCollectorSource;	
	    public FacetCountCollectorSource _collectAllSource = null;
	    public IFacetHandler facetHandler;
	    public RandomAccessFilter _filter;
	    public readonly CurrentPointers _currentPointers = new CurrentPointers();
	    public List<IFacetCountCollector> _countCollectorList = new List<IFacetCountCollector>();
	    public List<IFacetCountCollector> _collectAllCollectorList = new List<IFacetCountCollector>();

        public void SetNextReader(BoboIndexReader reader, int docBase)
        {
            if (_collectAllSource != null)
            {
                IFacetCountCollector collector = _collectAllSource.GetFacetCountCollector(reader, docBase);
                _collectAllCollectorList.Add(collector);
                collector.CollectAll();
            }
            else
            {
                if (_filter != null)
                {
                    _currentPointers.DocIdSet = _filter.GetRandomAccessDocIdSet(reader);
                    _currentPointers.PostDocIDSetIterator = _currentPointers.DocIdSet.Iterator();
                    _currentPointers.Doc = _currentPointers.PostDocIDSetIterator.NextDoc();
                }
                if (_facetCountCollectorSource != null)
                {
                    _currentPointers.FacetCountCollector = _facetCountCollectorSource.GetFacetCountCollector(reader, docBase);
                    _countCollectorList.Add(_currentPointers.FacetCountCollector);
                }
            }
        }

        public class CurrentPointers
        {
            public RandomAccessDocIdSet DocIdSet = null;
            public DocIdSetIterator PostDocIDSetIterator = null;
            public int Doc;
            public IFacetCountCollector FacetCountCollector;
        }
    }
}
