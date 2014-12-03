namespace BoboBrowse.Net.Util
{
    using Lucene.Net.Search;
    using System.Text;

    public static class DocIdSetUtil
    {
        public static string AsString(this DocIdSet docIdSet)
        {
            DocIdSetIterator iter = docIdSet.Iterator();
            StringBuilder buf = new StringBuilder();
            bool firstTime = true;
            buf.Append("[");
            while (iter.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
            {
                if (firstTime)
                {
                    firstTime = false;
                }
                else
                {
                    buf.Append(",");
                }
                buf.Append(iter.DocID());
            }
            buf.Append("]");
            return buf.ToString();
        }
    }
}
