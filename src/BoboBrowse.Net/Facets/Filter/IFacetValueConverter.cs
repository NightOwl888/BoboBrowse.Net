// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.Facets.Data;
    using System;

    public class FacetValueConverter_Fields
    {
        public static IFacetValueConverter DEFAULT = new DefaultFacetDataCacheConverter();

        public class DefaultFacetDataCacheConverter : IFacetValueConverter
        {		
		    public int[] Convert(FacetDataCache dataCache, string[] vals){
			    return FacetDataCache.Convert(dataCache, vals);
		    }
	    }
    }

    public interface IFacetValueConverter
    {
        int[] Convert(FacetDataCache dataCache, string[] vals);
    }
}
