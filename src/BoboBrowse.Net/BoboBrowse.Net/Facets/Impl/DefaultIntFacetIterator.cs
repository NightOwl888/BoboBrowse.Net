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

    public class DefaultIntFacetIterator : IntFacetIterator
    {
        private readonly TermIntList _valList;
        private BigSegmentedArray _count;
        private int _countlength;
        private int _countLengthMinusOne;
        private int _index;

        public DefaultIntFacetIterator(TermIntList valList, BigSegmentedArray countarray, int countlength, bool zeroBased)
        {
            _valList = valList;
            _count = countarray;
            _countlength = countlength;
            _countLengthMinusOne = countlength - 1;
            _index = -1;
            if (!zeroBased)
                _index++;
            _facet = TermIntList.VALUE_MISSING;
            count = 0;
        }

        /// <summary>
        /// Added in .NET version as as an accessor to the _valList field.
        /// </summary>
        public virtual TermIntList ValList
        {
            get { return _valList; }
        }

        new public virtual string Facet
        {
            get
            {
                if (_facet == -1) return null;
                return _valList.Format(_facet);
            }
        }

        public override string Format(int val)
        {
            return _valList.Format(val);
        }

        public override string Format(object val)
        {
            return _valList.Format(val);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacetCount()
        /// </summary>
        public virtual int FacetCount
        {
            get { return count; }
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#hasNext()
        /// </summary>
        /// <returns></returns>
        public override bool HasNext()
        {
            return (_index < _countLengthMinusOne);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see java.util.Iterator#next()
        /// </summary>
        /// <returns></returns>
        public override string Next()
        {
            if ((_index >= 0) && (_index >= _countLengthMinusOne))
                throw new IndexOutOfRangeException("No more facets in this iteration");
            _index++;
            _facet = _valList.GetPrimitiveValue(_index);
            count = _count.Get(_index);
            return _valList.Get(_index);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.IntFacetIterator#nextInt()
        /// </summary>
        /// <returns></returns>
        public override int NextInt()
        {
            if (_index >= _countLengthMinusOne)
                throw new IndexOutOfRangeException("No more facets in this iteration");
            _index++;
            _facet = _valList.GetPrimitiveValue(_index);
            count = _count.Get(_index);
            return _facet;
        }

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
                    _facet = _valList.GetPrimitiveValue(_index);
                    count = _count.Get(_index);
                    return _valList.Format(_facet);
                }
            }
            _facet = TermIntList.VALUE_MISSING;
            count = 0;
            return null;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.IntFacetIterator#nextInt(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override int NextInt(int minHits)
        {
            while (++_index < _countlength)
            {
                if (_count.Get(_index) >= minHits)
                {
                    _facet = _valList.GetPrimitiveValue(_index);
                    count = _count.Get(_index);
                    return _facet;
                }
            }
            _facet = TermIntList.VALUE_MISSING;
            count = 0;
            return _facet;    
        }
    }
}
