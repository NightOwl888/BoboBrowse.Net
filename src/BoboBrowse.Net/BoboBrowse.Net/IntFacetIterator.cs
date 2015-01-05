// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
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
