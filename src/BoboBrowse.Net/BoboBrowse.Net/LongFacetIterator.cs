// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu<xgu@linkedin.com>"
    /// </summary>
    public abstract class LongFacetIterator : FacetIterator
    {
        protected long _facet;

        new public virtual long Facet
        {
            get { return _facet; }
        }

        public abstract long NextLong();
        public abstract long NextLong(int minHits);
        public abstract string Format(long val);
    }
}
