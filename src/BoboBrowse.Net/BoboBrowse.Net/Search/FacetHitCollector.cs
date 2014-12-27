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
namespace BoboBrowse.Net.Search
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Search;
    using System;
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
