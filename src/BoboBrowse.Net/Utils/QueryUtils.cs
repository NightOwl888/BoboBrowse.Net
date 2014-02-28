

namespace BoboBrowse.Net.Utils
{
    using System;
    using System.Collections.Generic;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets;

    internal static class QueryUtils
    {
        internal static readonly SortField[] DEFAULT_SORT = new SortField[] { SortField.FIELD_SCORE };

        public static SortField[] convertSort(SortField[] sortSpec, BoboIndexReader idxReader)
        {
            SortField[] retVal = DEFAULT_SORT;
            if (sortSpec != null && sortSpec.Length > 0)
            {
                List<SortField> sortList = new List<SortField>(sortSpec.Length + 1);
                bool relevanceSortAdded = false;
                for (int i = 0; i < sortSpec.Length; ++i)
                {
                    if (SortField.FIELD_DOC.Equals(sortSpec[i]))
                    {
                        sortList.Add(SortField.FIELD_DOC);
                    }
                    else if (SortField.FIELD_SCORE.Equals(sortSpec[i]))
                    {
                        sortList.Add(SortField.FIELD_SCORE);
                        relevanceSortAdded = true;
                    }
                    else
                    {
                        string fieldname = sortSpec[i].Field;
                        if (fieldname != null)
                        {
                            SortField sf = null;
                            FacetHandler facetHandler = idxReader.GetFacetHandler(fieldname);
                            if (facetHandler != null)
                            {
                                sf = new SortField(fieldname.ToLower(), new QuerySortComparatorSource(facetHandler), sortSpec[i].Reverse);
                            }
                            else
                            {
                                sf = sortSpec[i];
                            }
                            sortList.Add(sf);
                        }
                    }
                }
                if (!relevanceSortAdded)
                {
                    sortList.Add(SortField.FIELD_SCORE);
                }
                retVal = sortList.ToArray();
            }
            return retVal;
        }

        private class QuerySortComparatorSource : FieldComparatorSource
        {
            private FacetHandler facetHandler;

            public QuerySortComparatorSource(FacetHandler facetHandler)
            {
                this.facetHandler = facetHandler;
            }

            public override FieldComparator NewComparator(string fieldname, int numHits, int sortPos, bool reversed)
            {
                return facetHandler.GetScoreDocComparator();
            }
        }
    }
}
