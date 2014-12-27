// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Sort;
    using Lucene.Net.Search;
    using System;

    public class BoboCustomSortField : SortField
    {
        private static long serialVersionUID = 1L;

	    private readonly DocComparatorSource _factory;

        public BoboCustomSortField(string field, bool reverse, DocComparatorSource factory)
            : base(field, SortField.CUSTOM, reverse)
        {
            _factory = factory;
        }

        public virtual DocComparatorSource GetCustomComparatorSource()
        {
            return _factory;
        }
    }
}
