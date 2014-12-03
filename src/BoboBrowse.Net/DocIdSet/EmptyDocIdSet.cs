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

namespace BoboBrowse.Net.DocIdSet
{
    using System;
    using Lucene.Net.Search;

    public class EmptyDocIdSet : RandomAccessDocIdSet
    {
        private static EmptyDocIdSet SINGLETON = new EmptyDocIdSet();

        private class EmptyDocIdSetIterator : DocIdSetIterator
        {
            public override int Advance(int target)
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }

            public override int DocID()
            {
                return -1;
            }

            public override int NextDoc()
            {
                return DocIdSetIterator.NO_MORE_DOCS;
            }
        }

        private static EmptyDocIdSetIterator SINGLETON_ITERATOR = new EmptyDocIdSetIterator();

        private EmptyDocIdSet()
        {
        }

        public static EmptyDocIdSet GetInstance()
        {
            return SINGLETON;
        }

        public override DocIdSetIterator Iterator()
        {
            return SINGLETON_ITERATOR;
        }

        public override bool Get(int docId)
        {
            return false;
        }
    }
}
