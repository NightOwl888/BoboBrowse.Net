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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Search;
    using LuceneExt.Impl;

    public class RandomAccessNotFilter : RandomAccessFilter
    {
        //private static long serialVersionUID = 1L; // NOT USED

        protected readonly RandomAccessFilter _innerFilter;

        public RandomAccessNotFilter(RandomAccessFilter innerFilter)
        {
            _innerFilter = innerFilter;
        }

        public override double GetFacetSelectivity(BoboIndexReader reader)
        {
            double selectivity = _innerFilter.GetFacetSelectivity(reader);
            selectivity = selectivity > 0.999 ? 0.0 : (1 - selectivity);
            return selectivity;
        }

        public override RandomAccessDocIdSet GetRandomAccessDocIdSet(BoboIndexReader reader)
        {
            RandomAccessDocIdSet innerDocIdSet = _innerFilter.GetRandomAccessDocIdSet(reader);
            DocIdSet notInnerDocIdSet = new NotDocIdSet(innerDocIdSet, reader.MaxDoc);
            return new NotRandomAccessDocIdSet(innerDocIdSet, notInnerDocIdSet);
        }

        private class NotRandomAccessDocIdSet : RandomAccessDocIdSet
        {
            private readonly RandomAccessDocIdSet innerDocIdSet;
            private readonly DocIdSet notInnerDocIdSet;

            public NotRandomAccessDocIdSet(RandomAccessDocIdSet innerDocIdSet, DocIdSet notInnerDocIdSet)
            {
                this.innerDocIdSet = innerDocIdSet;
                this.notInnerDocIdSet = notInnerDocIdSet;
            }

            public override bool Get(int docId)
            {
                return !innerDocIdSet.Get(docId);
            }
            public override DocIdSetIterator Iterator()
            {
                return notInnerDocIdSet.Iterator();
            }
        }
    }
}
