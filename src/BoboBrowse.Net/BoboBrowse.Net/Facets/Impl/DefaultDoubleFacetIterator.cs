// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public class DefaultDoubleFacetIterator : DoubleFacetIterator
    {
        public TermDoubleList _valList;
        private int[] _count;
        private int _countlength;
        private int _countLengthMinusOne;
        private int _index;

        public DefaultDoubleFacetIterator(TermDoubleList valList, int[] countarray, int countlength, bool zeroBased)
        {
            _valList = valList;
            _countlength = countlength;
            _count = countarray;
            _countLengthMinusOne = _countlength - 1;
            _index = -1;
            if (!zeroBased)
                _index++;
            base._facet = TermDoubleList.VALUE_MISSING;
            base.count = 0;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.FacetIterator#getFacet()
        /// </summary>
        new public virtual string Facet
        {
            get
            {
                if (base._facet == TermDoubleList.VALUE_MISSING) return null;
                return _valList.Format(base._facet);
            }
        }

        public override string Format(double val)
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
            get { return base.count; }
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
            base.count = _count[_index];
            return _valList.Get(_index);
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.DoubleFacetIterator#nextDouble()
        /// </summary>
        /// <returns></returns>
        public override double NextDouble()
        {
            if (_index >= _countLengthMinusOne)
                throw new IndexOutOfRangeException("No more facets in this iteration");
            _index++;
            _facet = _valList.GetPrimitiveValue(_index);
            base.count = _count[_index];
            return _facet;
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
                if (_count[_index] >= minHits)
                {
                    _facet = _valList.GetPrimitiveValue(_index);
                    base.count = _count[_index];
                    return _valList.Format(_facet);
                }
            }
            _facet = TermDoubleList.VALUE_MISSING;
            base.count = 0;
            return null;
        }

        /// <summary>
        /// (non-Javadoc)
        /// see com.browseengine.bobo.api.DoubleFacetIterator#nextDouble(int)
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns></returns>
        public override double NextDouble(int minHits)
        {
            while (++_index < _countlength)
            {
                if (_count[_index] >= minHits)
                {
                    _facet = _valList.GetPrimitiveValue(_index);
                    base.count = _count[_index];
                    return _facet;
                }
            }
            _facet = TermDoubleList.VALUE_MISSING;
            base.count = 0;
            return _facet;
        }
    }
}
