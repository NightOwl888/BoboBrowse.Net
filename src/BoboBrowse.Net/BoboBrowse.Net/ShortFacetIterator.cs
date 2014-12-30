// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using System;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public abstract class ShortFacetIterator : FacetIterator
    {
        public short _facet;

        new public virtual short Facet
        {
            get { return _facet; }
        }

        public abstract short NextShort();
        public abstract short NextShort(int minHits);
        public abstract string Format(short val);
    }
}
