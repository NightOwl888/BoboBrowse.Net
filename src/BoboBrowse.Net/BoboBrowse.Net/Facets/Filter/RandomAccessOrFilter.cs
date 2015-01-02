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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Search;
    using LuceneExt.Impl;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class RandomAccessOrFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly IEnumerable<RandomAccessFilter> _filters;

        public RandomAccessOrFilter(IEnumerable<RandomAccessFilter> filters)
        {
            if (filters == null)
            {
                throw new ArgumentNullException("filters");
            }
            _filters = filters;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = 0;
            foreach (RandomAccessFilter filter in _filters)
            {
                selectivity += filter.GetFacetSelectivity(reader);
            }
    
            if(selectivity > 0.999)
            {
                selectivity = 1.0;
            }
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            var count = _filters.Count();
            if (count == 1)
            {
                return _filters.ElementAt(0).GetRandomAccessDocIdSet(reader);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(count);
                List<RandomAccessDocIdSet> randomAccessList = new List<RandomAccessDocIdSet>(count);
                foreach (RandomAccessFilter f in _filters)
                {
                    RandomAccessDocIdSet s = f.GetRandomAccessDocIdSet(reader);
                    list.Add(s);
                    randomAccessList.Add(s);
                }
                RandomAccessDocIdSet[] randomAccessDocIdSets = randomAccessList.ToArray();
                DocIdSet orDocIdSet = new OrDocIdSet(list);
                return new RandomOrFilterDocIdSet(randomAccessDocIdSets, orDocIdSet);
            }
        }

        private class RandomOrFilterDocIdSet : RandomAccessDocIdSet
        {
            private RandomAccessDocIdSet[] randomAccessDocIdSets;
            private DocIdSet orDocIdSet;

            public RandomOrFilterDocIdSet(RandomAccessDocIdSet[] randomAccessDocIdSets, DocIdSet orDocIdSet)
            {
                this.orDocIdSet = orDocIdSet;
                this.randomAccessDocIdSets = randomAccessDocIdSets;
            }
            public override bool Get(int docId)
            {
                foreach (RandomAccessDocIdSet s in randomAccessDocIdSets)
                {
                    if (s.Get(docId))
                        return true;
                }
                return false;
            }

            public override DocIdSetIterator Iterator()
            {
                return orDocIdSet.Iterator();
            }
        }  
    }
}
