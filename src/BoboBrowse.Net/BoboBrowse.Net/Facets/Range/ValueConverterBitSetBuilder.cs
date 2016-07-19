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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Range
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Util;
    using System;

    public class ValueConverterBitSetBuilder : IBitSetBuilder
    {
        private readonly IFacetValueConverter facetValueConverter;
        private readonly string[] vals;
        private readonly bool takeCompliment;

        public ValueConverterBitSetBuilder(IFacetValueConverter facetValueConverter, string[] vals, bool takeCompliment) 
        {
            this.facetValueConverter = facetValueConverter;
            this.vals = vals;
            this.takeCompliment = takeCompliment;    
        }

        public virtual OpenBitSet BitSet(FacetDataCache dataCache)
        {
            int[] index = facetValueConverter.Convert(dataCache, vals);

            OpenBitSet bitset = new OpenBitSet(dataCache.ValArray.Count);
            foreach (int i in index)
            {
                bitset.FastSet(i);
            }
            if (takeCompliment)
            {
                // flip the bits
                for (int i = 0; i < index.Length; ++i)
                {
                    bitset.FastFlip(i);
                }
            }
            return bitset;
        }
    }
}
