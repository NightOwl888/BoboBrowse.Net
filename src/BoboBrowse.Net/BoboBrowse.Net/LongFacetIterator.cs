// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
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
