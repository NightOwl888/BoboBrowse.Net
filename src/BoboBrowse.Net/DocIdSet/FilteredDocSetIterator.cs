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

namespace BoboBrowse.Net.DocIdSet
{
    using System;
    using Lucene.Net.Search;

    public abstract class FilteredDocSetIterator : DocIdSetIterator
    {
        protected internal DocIdSetIterator innerIter;
        private int currentDoc;

        protected FilteredDocSetIterator(DocIdSetIterator innerIter)
        {
            if (innerIter == null)
            {
                throw new System.ArgumentException("null iterator");
            }
            this.innerIter = innerIter;
            currentDoc = -1;
        }

        protected internal abstract bool Match(int doc);

        public override int Advance(int target)
        {
            bool flag = innerIter.Advance(target) != DocIdSetIterator.NO_MORE_DOCS;
            if (flag)
            {
                int doc = innerIter.DocID();
                if (Match(doc))
                {
                    currentDoc = doc;
                    return currentDoc;
                }
                else
                {
                    while (innerIter.NextDoc()!=DocIdSetIterator.NO_MORE_DOCS)
                    {
                        int docid = innerIter.DocID();
                        if (Match(docid))
                        {
                            currentDoc = docid;
                            return currentDoc;
                        }
                    }                   
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }

        public override int DocID()
        {
            return currentDoc;
        }

        public override int NextDoc()
        {
            while (innerIter.NextDoc()!=DocIdSetIterator.NO_MORE_DOCS)
            {
                int doc = innerIter.DocID();
                if (Match(doc))
                {
                    currentDoc = doc;
                    return currentDoc;
                }
            }
            return DocIdSetIterator.NO_MORE_DOCS;
        }
    }
}
