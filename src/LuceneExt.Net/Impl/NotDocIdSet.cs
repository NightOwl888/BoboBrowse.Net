// Copyright (c) COMPANY. All rights reserved. 
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
    using System.IO;
    using Lucene.Net.Search;


    [Serializable]
    public class NotDocIdSet : ImmutableDocSet
    {       
        private readonly DocIdSet innerSet = null;

        private readonly int max = -1;

        public NotDocIdSet(DocIdSet docSet, int maxVal)
        {
            innerSet = docSet;
            max = maxVal;
        }

        internal class NotDocIdSetIterator : DocIdSetIterator
        {
            internal int lastReturn = -1;
            private DocIdSetIterator it1 = null;
            private int innerDocid = -1;
            private NotDocIdSet parent;

            internal NotDocIdSetIterator(NotDocIdSet parent)
            {
                this.parent = parent;
                Initialize();
            }

            private void Initialize()
            {
                it1 = parent.innerSet.Iterator();

                try
                {
                    if ((innerDocid = it1.NextDoc()) == DocIdSetIterator.NO_MORE_DOCS)
                        it1 = null;
                }
                catch (IOException e)
                {                 
                }
            }

            public override int DocID()
            {
                return lastReturn;
            }

            public override int NextDoc()
            {
                return Advance(0);
            }

            public override int Advance(int target)
            {
                if (lastReturn == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return DocIdSetIterator.NO_MORE_DOCS;
                }

                if (target <= lastReturn)
                    target = lastReturn + 1;

                if (it1 != null && innerDocid < target)
                {
                    if ((innerDocid = it1.Advance(target)) == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        it1 = null;
                    }
                }

                while (it1 != null && innerDocid == target)
                {
                    target++;
                    if (target >= parent.max)
                    {
                        return (lastReturn = DocIdSetIterator.NO_MORE_DOCS);
                    }
                    if ((innerDocid = it1.Advance(target)) == DocIdSetIterator.NO_MORE_DOCS)
                    {
                        it1 = null;
                    }
                }
                return (lastReturn = target);
            }
        }

        public override DocIdSetIterator Iterator()
        {
            return new NotDocIdSetIterator(this);
        }

        ///  
        ///<summary>Find existence in the set with index
        ///   * 
        ///   * NOTE :  Expensive call. Avoid. </summary>
        ///   * <param name="val"> value to find the index for </param>
        ///   * <returns> index if the given value </returns>
        ///   
        public override int FindWithIndex(int val)
        {
            DocIdSetIterator finder = new NotDocIdSetIterator(this);
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
    }
}
