// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// author nnarkhed
    /// </summary>
    public class DefaultFacetIterator : FacetIterator
    {
        private ITermValueList _valList;
        private int[] _count;
        private int _countlength;
        private int _index;
        private int _lastIndex;

        public DefaultFacetIterator(ITermValueList valList, int[] counts, int countlength, bool zeroBased)
        {
            _valList = valList;
            _count = counts;
            _countlength = countlength;
            _index = -1;
            _lastIndex = _countlength - 1;
            if (!zeroBased)
                _index++;
            facet = null;
            base.count = 0;
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
            // TODO: Try to find a way to get this value without having to cast.
            facet = Convert.ToString(_valList.GetRawValue(_index));
            base.count = _count[_index];
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
                if (_count[_index] >= minHits)
                {
                    // TODO: Try to find a way to get this value without having to cast.
                    facet = Convert.ToString(_valList.GetRawValue(_index));
                    base.count = _count[_index];
                    return Format(facet);
                }
            }
            facet = null;
            base.count = 0;
            return null;   
        }

        public override string Format(object val)
        {
            return _valList.Format(val);
        }
    }
}
