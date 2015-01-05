// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
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
