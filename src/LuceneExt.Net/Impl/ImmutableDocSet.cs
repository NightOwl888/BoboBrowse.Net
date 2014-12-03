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

    public abstract class ImmutableDocSet : DocSet
    {
        private int size = -1;

        public override void AddDoc(int docid)
        {
            throw new NotSupportedException("Attempt to add document to an immutable data structure");
        }

        public override int Size()
        {
            // Do the size if we haven't done it so far.
            if (size < 0)
            {
                DocIdSetIterator dcit = Iterator();
                size = 0;
                try
                {
                    while (dcit.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        size++;
                    }
                }
                catch (IOException e)
                {                    
                    return -1;
                }
            }
            return size;
        }
    }
}
