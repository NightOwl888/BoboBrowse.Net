//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2015  Shad Storhaug
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

namespace BoboBrowse.Net.Support
{
    using Lucene.Net.Search;

    /// <summary>
    /// Class to replace DocIdSet.EMPTY_DOCIDSET.Iterator(), since we need
    /// to make empty iterators in BoboBrowse, but this feature seems like it is no longer
    /// available in Lucene 4.8.0.
    /// </summary>
    public class EmptyDocIdSetIterator : DocIdSetIterator
    {
        public override int Advance(int target)
        {
            return NO_MORE_DOCS;
        }

        public override int DocID()
        {
            return NO_MORE_DOCS;
        }

        public override int NextDoc()
        {
            return NO_MORE_DOCS;
        }

        public override long Cost()
        {
            return 0;
        }
    }
}
