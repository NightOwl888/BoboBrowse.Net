// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Attribute
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Facets.Range;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Index;
    using System.Collections.Generic;

    public class AttributesFacetHandler : MultiRangeFacetHandler
    {
        public const char DEFAULT_SEPARATOR = '=';
        private char separator;
        private int numFacetsPerKey = 7;
        public const string SEPARATOR_PROP_NAME = "separator";
        public const string MAX_FACETS_PER_KEY_PROP_NAME = "maxFacetsPerKey";

        public AttributesFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, Term sizePayloadTerm, IDictionary<string, string> facetProps)
            : base(name, indexFieldName, sizePayloadTerm, termListFactory, new string[0])
        {
            if (facetProps.ContainsKey(SEPARATOR_PROP_NAME))
            {
                this.separator = Narrow(facetProps.Get(SEPARATOR_PROP_NAME))[0];
            }
            else
            {
                this.separator = DEFAULT_SEPARATOR;
            }
            if (facetProps.ContainsKey(MAX_FACETS_PER_KEY_PROP_NAME))
            {
                this.numFacetsPerKey = int.Parse(Narrow(facetProps.Get(MAX_FACETS_PER_KEY_PROP_NAME)));
            }
        }

        private string Narrow(string @string)
        {
            return @string.Replace("\\[", "").Replace("\\]", "");
        }

        public virtual char GetSeparator(BrowseSelection browseSelection)
        {
            if (browseSelection == null || !browseSelection.SelectionProperties.ContainsKey(SEPARATOR_PROP_NAME))
            {
                return separator;
            }
            return browseSelection.SelectionProperties.Get(SEPARATOR_PROP_NAME)[0];
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> prop)
        {
            return base.BuildRandomAccessFilter(ConvertToRangeString(value, separator), prop);
        }

        public static string ConvertToRangeString(string key, char separator)
        {
            if (key.StartsWith("[") && key.Contains(" TO "))
            {
                return key;
            }
            return "[" + key + separator + " TO " + key + (char)(separator + 1) + ")";
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            string[] ranges = new string[vals.Length];
            for (int i = 0; i < vals.Length; i++)
            {
                ranges[i] = ConvertToRangeString(vals[i], separator);
            }
            return base.BuildRandomAccessOrFilter(ranges, prop, isNot);
        }

        public virtual int GetFacetsPerKey(BrowseSelection browseSelection)
        {
            if (browseSelection == null || !browseSelection.SelectionProperties.ContainsKey(MAX_FACETS_PER_KEY_PROP_NAME))
            {
                return numFacetsPerKey;
            }
            return int.Parse(browseSelection.SelectionProperties.Get(MAX_FACETS_PER_KEY_PROP_NAME));
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec ospec)
        {
            return new AttributesFacetCountCollectorSource(this, sel, ospec);
        }

        public class AttributesFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly BrowseSelection _browseSelection;
            private readonly FacetSpec _ospec;
            private readonly AttributesFacetHandler _parent;

            public AttributesFacetCountCollectorSource(AttributesFacetHandler parent, BrowseSelection browseSelection, FacetSpec ospec)
            {
                _parent = parent;
                _browseSelection = browseSelection;
                _ospec = ospec;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                int facetsPerKey = _parent.GetFacetsPerKey(_browseSelection);
                if (_ospec.Properties != null && _ospec.Properties.ContainsKey(MAX_FACETS_PER_KEY_PROP_NAME))
                {
                    facetsPerKey = int.Parse(_ospec.Properties.Get(MAX_FACETS_PER_KEY_PROP_NAME));
                }
                MultiValueFacetDataCache dataCache = (MultiValueFacetDataCache)reader.GetFacetData(_parent.Name);
                return new AttributesFacetCountCollector(_parent, _parent.Name, dataCache, docBase, _browseSelection, _ospec, facetsPerKey, _parent.GetSeparator(_browseSelection));
            }
        }
    }
}
