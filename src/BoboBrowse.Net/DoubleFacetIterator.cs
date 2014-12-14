// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using System;

    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public abstract class DoubleFacetIterator : FacetIterator
    {
        protected double _facet;

        new public double Facet
        {
            get { return _facet; }
        }

        public abstract double NextDouble();
        public abstract double NextDouble(int minHits);
        public abstract string Format(double val);
    }
}
