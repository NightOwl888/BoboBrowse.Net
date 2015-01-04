// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
    /// </summary>
    public abstract class DoubleFacetIterator : FacetIterator
    {
        protected double _facet;

        new public virtual double Facet
        {
            get { return _facet; }
        }

        public abstract double NextDouble();
        public abstract double NextDouble(int minHits);
        public abstract string Format(double val);
    }
}
