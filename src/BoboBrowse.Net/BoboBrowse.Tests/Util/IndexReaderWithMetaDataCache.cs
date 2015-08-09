//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Util
{
    using BoboBrowse.Net.Search.Section;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Index;
    using System.Collections.Generic;

    public class IndexReaderWithMetaDataCache : FilterIndexReader, IMetaDataCacheProvider
    {
        private static Term intMetaTerm = new Term("metafield", "intmeta");
        private IDictionary<Term, IMetaDataCache> map = new Dictionary<Term, IMetaDataCache>();

        public IndexReaderWithMetaDataCache(IndexReader @in)
            : base(@in)
        {
            map.Put(intMetaTerm, new IntMetaDataCache(intMetaTerm, @in));
        }

        public IMetaDataCache Get(Term term)
        {
            return map.Get(term);
        }
    }
}
