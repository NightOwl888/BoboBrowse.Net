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

namespace BoboBrowse.Net.Facets.Filters
{
    using System;
    using System.Collections.Generic;
    using Lucene.Net.Search;

    public class FilterMapFacetHandler : FacetHandler
    {
        protected internal readonly Dictionary<string, FacetEntry> _filterMap;
        protected internal readonly FacetEntry[] _facetEntries;
        protected internal BoboIndexReader _reader;

        public FilterMapFacetHandler(string name, Dictionary<string, RandomAccessFilter> filterMap)
            : base(name)
        {
            _facetEntries = new FacetEntry[filterMap.Count];
            _filterMap = new Dictionary<string, FacetEntry>();
            int i = 0;
            foreach (KeyValuePair<string, RandomAccessFilter> entry in filterMap)
            {
                FacetEntry f = new FacetEntry();
                f.filter = entry.Value;
                f.value = entry.Key;
                _facetEntries[i] = f;
                _filterMap.Add(f.value, f);
                i++;
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties props)
        {
            return _filterMap[@value].filter;
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
        {
            return new FilterMapFacetCountCollector(this);
        }

        public override string[] GetFieldValues(int id)
        {
            List<string> values = new List<string>();
            foreach (FacetEntry entry in _facetEntries)
            {
                if (entry.docIdSet.Get(id))
                    values.Add(entry.value);
            }
            return values.Count > 0 ? values.ToArray() : null;
        }

        public override FieldComparator GetScoreDocComparator()
        {
            return null;
        }

        public override void Load(BoboIndexReader reader)
        {
            _reader = reader;
            foreach (FacetEntry entry in _facetEntries)
                entry.docIdSet = entry.filter.GetRandomAccessDocIdSet(reader);
        }

        protected internal class FacetEntry
        {
            internal string @value;
            internal RandomAccessFilter filter;
            internal RandomAccessDocIdSet docIdSet;
        }

        protected internal class FilterMapFacetCountCollector : IFacetCountCollector
        {
            private FilterMapFacetHandler parent;
            private int[] _counts;

            public FilterMapFacetCountCollector(FilterMapFacetHandler parent)
            {
                this.parent = parent;
                _counts = new int[parent._facetEntries.Length];
            }

            public virtual int[] GetCountDistribution()
            {
                return _counts;
            }

            public virtual void Collect(int docid)
            {
                for (int i = 0; i < parent._facetEntries.Length; i++)
                {
                    if (parent._facetEntries[i].docIdSet.Get(docid))
                        _counts[i]++;
                }
            }

            public virtual void CollectAll()
            {
                throw new InvalidOperationException("not supported");
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                List<BrowseFacet> facets = new List<BrowseFacet>();
                for (int i = 0; i < parent._facetEntries.Length; i++)
                {
                    FacetEntry entry = parent._facetEntries[i];
                    BrowseFacet facet = new BrowseFacet();
                    facet.HitCount = _counts[i];
                    facet.Value = entry.value;
                    facets.Add(facet);
                }
                return facets;
            }

            public virtual string Name
            {
                get
                {
                    return parent.Name;
                }
            }

            public virtual List<BrowseFacet> combine(BrowseFacet facet, List<BrowseFacet> facets)
            {
                // TODO Auto-generated method stub
                return null;
            }

            public virtual BrowseFacet GetFacet(string @value)
            {
                // TODO Auto-generated method stub
                return null;
            }

        }

        public override object[] GetRawFieldValues(int id)
        {
            return GetFieldValues(id);
        }
    }
}
