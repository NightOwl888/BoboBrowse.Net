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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Support;
    using System.Collections.Generic;

    public class MultiValueWithWeightFacetHandler : MultiValueFacetHandler
    {
        public MultiValueWithWeightFacetHandler(string name, string indexFieldName, TermListFactory termListFactory)
            : base(name, indexFieldName, termListFactory, null, null)
        {
        }

        public MultiValueWithWeightFacetHandler(string name, string indexFieldName)
            : base(name, indexFieldName, null, null, null)
        {
        }

        public MultiValueWithWeightFacetHandler(string name)
            : base(name, name, null, null, null)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            MultiValueFacetFilter f = new MultiValueFacetFilter(new MultiDataCacheBuilder(Name, _indexFieldName), value);
            return f;
        }

        public override MultiValueFacetDataCache Load(BoboIndexReader reader, BoboIndexReader.WorkArea workArea)
        {
            MultiValueWithWeightFacetDataCache dataCache = new MultiValueWithWeightFacetDataCache();

            dataCache.MaxItems = _maxItems;

            if (_sizePayloadTerm == null)
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, workArea);
            }
            else
            {
                dataCache.Load(_indexFieldName, reader, _termListFactory, _sizePayloadTerm);
            }
            return dataCache;
        }
    }
}
