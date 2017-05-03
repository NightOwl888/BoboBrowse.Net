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
namespace BoboBrowse.Net.Search
{
    using BoboBrowse.Net.DocIdSet;
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Search;
    using System.Collections.Generic;
    
    public sealed class FacetHitCollector
    {
        public FacetCountCollectorSource FacetCountCollectorSource { get; set; }
	    public FacetCountCollectorSource CollectAllSource { get; set; } = null;
	    public IFacetHandler FacetHandler { get; set; }
        public RandomAccessFilter Filter { get; set; }
        public CurrentPointers CurrentPointers { get; private set; } = new CurrentPointers();
	    public IList<IFacetCountCollector> CountCollectorList { get; set; } = new List<IFacetCountCollector>();
	    public IList<IFacetCountCollector> CollectAllCollectorList { get; set; } = new List<IFacetCountCollector>();

        public void SetNextReader(BoboSegmentReader reader, int docBase)
        {
            if (CollectAllSource != null)
            {
                IFacetCountCollector collector = CollectAllSource.GetFacetCountCollector(reader, docBase);
                CollectAllCollectorList.Add(collector);
                collector.CollectAll();
            }
            else
            {
                if (Filter != null)
                {
                    CurrentPointers.DocIdSet = Filter.GetRandomAccessDocIdSet(reader);
                    CurrentPointers.PostDocIDSetIterator = CurrentPointers.DocIdSet.GetIterator();
                    CurrentPointers.Doc = CurrentPointers.PostDocIDSetIterator.NextDoc();
                }
                if (FacetCountCollectorSource != null)
                {
                    CurrentPointers.FacetCountCollector = FacetCountCollectorSource.GetFacetCountCollector(reader, docBase);
                    CountCollectorList.Add(CurrentPointers.FacetCountCollector);
                }
            }
        }
    }

    // BoboBrowse.Net: de-nested this type from FacetHitCollector to prevent naming collision with property.
    public class CurrentPointers
    {
        public RandomAccessDocIdSet DocIdSet { get; set; } = null;
        public DocIdSetIterator PostDocIDSetIterator { get; set; } = null;
        public int Doc { get; set; }
        public IFacetCountCollector FacetCountCollector { get; set; }
    }
}
