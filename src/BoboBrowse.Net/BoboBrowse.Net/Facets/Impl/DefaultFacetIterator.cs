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
    using BoboBrowse.Net.Util;
    using System;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class DefaultFacetIterator : FacetIterator
    {
        private readonly ITermValueList _valList;
        private BigSegmentedArray _count;
        private int _countlength;
        private int _index;
        private int _lastIndex;

        public DefaultFacetIterator(ITermValueList valList, BigSegmentedArray counts, int countlength, bool zeroBased)
        {
            _valList = valList;
            _count = counts;
            _countlength = countlength;
            _index = -1;
            _lastIndex = _countlength - 1;
            if (!zeroBased)
                _index++;
            facet = null;
            count = 0;
        }

        /// <summary>
        /// Added in .NET version as as an accessor to the _valList field.
        /// </summary>
        public virtual ITermValueList ValList
        {
            get { return _valList; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (_index < _lastIndex);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            _index++;
            facet = Convert.ToString(_valList.GetRawValue(_index));
            count = _count.Get(_index);
            return Format(facet);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#remove()
        /// </summary>
        public override void Remove()
        {
            throw new NotSupportedException("remove() method not supported for Facet Iterators");
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#next(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override string Next(int minHits)
        {
            while (++_index < _countlength)
            {
                if (_count.Get(_index) >= minHits)
                {
                    facet = Convert.ToString(_valList.GetRawValue(_index));
                    count = _count.Get(_index);
                    return Format(facet);
                }
            }
            facet = null;
            count = 0;
            return null;   
        }

        public override string Format(object val)
        {
            return _valList.Format(val);
        }
    }
}
