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
        private ITermValueList _valList;
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
