// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    /// <summary>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
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
