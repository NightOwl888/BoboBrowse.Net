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

namespace LuceneExt.Impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Lucene.Net.Search;

    [Serializable]
    public class OrDocIdSet : ImmutableDocSet
    {
        private const int INVALID = -1;
       
        [Serializable]
        public class AescDocIdSetComparator : IComparer<DocIdSetIterator>
        {
            public virtual int Compare(DocIdSetIterator o1, DocIdSetIterator o2)
            {
                return o1.DocID() - o2.DocID();
            }
        }

        internal List<DocIdSet> sets;

        private int size = INVALID;

        public OrDocIdSet(List<DocIdSet> docSets)
        {
            this.sets = docSets;
            int size = 0;
            if (sets != null)
            {
                foreach (DocIdSet set in sets)
                {
                    if (set != null)
                    {
                        size++;
                    }
                }
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new OrDocIdSetIterator(sets);
            //    
            //    List<DocIdSetIterator> list = new ArrayList<DocIdSetIterator>(sets.size());
            //    for (DocIdSet set : sets)
            //    {
            //      list.add(set.Iterator());
            //    }
            //    return new DisjunctionDISI(list);
            //    
        }


        ///<summary>Find existence in the set with index
        ///   * NOTE :  Expensive call. Avoid. </summary>
        ///   * <param name="val"> value to find the index for </param>
        ///   * <returns> index where the value is </returns>
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new OrDocIdSetIterator(sets);
            int cursor = -1;
            try
            {
                int docid;

                while ((docid = finder.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    if (docid > val)
                    {
                        return -1;
                    }
                    else if (docid == val)
                    {
                        return ++cursor;
                    }
                    else
                    {
                        ++cursor;
                    }
                }
            }
            catch (IOException e)
            {
                return -1;
            }
            return -1;
        }

        public override int Size()
        {
            if (size == INVALID)
            {
                size = 0;
                DocIdSetIterator it = this.Iterator();

                try
                {
                    while (it.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        size++;
                }
                catch (IOException e)
                {                    
                    size = INVALID;
                }
            }
            return size;
        }
    }
}
