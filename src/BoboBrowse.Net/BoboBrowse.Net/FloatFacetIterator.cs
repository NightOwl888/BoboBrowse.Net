// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using System;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public abstract class FloatFacetIterator : FacetIterator
    {
        protected float _facet;

        new public virtual float Facet
        {
            get { return _facet; }
        }

        public abstract float NextFloat();
        public abstract float NextFloat(int minHits);
        public abstract string Format(float val);
    }
}
