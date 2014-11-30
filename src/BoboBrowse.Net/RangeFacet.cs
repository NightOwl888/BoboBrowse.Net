namespace BoboBrowse.Net
{
    using System;
    using Lucene.Net.Documents;

    public class RangeFacet : BrowseFacet
    {
        public object Lower { get; set; }
        public object Upper { get; set; }

        internal void SetValues(object lower, object upper)
        {
            Lower = lower;
            Upper = upper;
            if (lower is DateTime)
            {
                lower = DateTools.DateToString((DateTime)lower, DateTools.Resolution.MINUTE);
                upper = DateTools.DateToString((DateTime)upper, DateTools.Resolution.MINUTE);
            }
            Value = string.Concat("[", lower, " TO ", upper, "]");
        }
    }
}
