
namespace BoboBrowse.Net
{
    using System;
    using System.Collections.Generic;

    public interface IComparatorFactory
    {
        ///<summary>Providers a Comparator from field values and counts. This is called within a browse. </summary>
        ///<param name="fieldValueAccessor"> accessor for field values </param>
        ///<param name="counts"> hit counts </param>
        ///<returns> Comparator instance </returns>
        IComparer<int> NewComparator(IFieldValueAccessor fieldValueAccessor, int[] counts);

        ///<summary>Providers a Comparator. This is called when doing a merge across browses. </summary>
        ///<returns> Comparator instance </returns>
        IComparer<BrowseFacet> NewComparator();
    }
}
