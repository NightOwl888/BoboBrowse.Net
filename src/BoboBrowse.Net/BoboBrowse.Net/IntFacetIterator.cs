// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using System;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public abstract class IntFacetIterator : FacetIterator
    {
        protected int _facet;

        new public virtual int Facet
        {
            get { return _facet; }
        }

        public abstract int NextInt();
        public abstract int NextInt(int minHits);
        public abstract string Format(int val);
    }
}
