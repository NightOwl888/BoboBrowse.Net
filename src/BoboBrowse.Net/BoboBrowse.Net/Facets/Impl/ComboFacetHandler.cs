// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class ComboFacetHandler : FacetHandler<FacetDataNone>
    {
        private const string DFEAULT_SEPARATOR = ":";
	    private readonly string _separator;

        /// <summary>
        /// Initializes a new instance of <see cref="T:ComboFacetHandler"/>. The separator will be assumed to be ":".
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public ComboFacetHandler(string name, IEnumerable<string> dependsOn)
            : this(name, DFEAULT_SEPARATOR, dependsOn)
        {}

        /// <summary>
        /// Initializes a new instance of <see cref="T:ComboFacetHandler"/>.
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="separator">The separator that is used to delineate the values of the different index fields.</param>
        /// <param name="dependsOn">List of facets this one depends on for loading.</param>
        public ComboFacetHandler(string name, string separator, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {
            _separator = separator;
        }

        public virtual string Separator
        {
            get { return _separator; }
        }

        private class ComboSelection
        {
            private readonly string name;
		    private readonly string val;

            public string Name
            {
                get { return this.name; }
            }

            public string Value
            {
                get { return this.val; }
            }

            private ComboSelection(string name, string val)
            {
                this.name = name;
                this.val = val;
            }

            public static ComboSelection Parse(string value, string sep)
            {
                var splitString = value.Split(new string[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                string name = splitString.Length > 0 ? splitString[0] : null;
                string val = splitString.Length > 1 ? splitString[1] : null;

                if (name != null && val != null)
                {
                    return new ComboSelection(name, val);
                }
                return null;
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties selectionProperty)
        {
            RandomAccessFilter retFilter = EmptyFilter.Instance;
            ComboSelection comboSel = ComboSelection.Parse(value, _separator);
            if (comboSel != null)
            {
                IFacetHandler handler = GetDependedFacetHandler(comboSel.Name);
                if (handler != null)
                {
                    retFilter = handler.BuildRandomAccessFilter(comboSel.Value, selectionProperty);
                }
            }
            return retFilter;
        }

        private static IDictionary<string, IList<string>> ConvertMap(string[] vals, string sep)
        {
            IDictionary<string, IList<string>> retmap = new Dictionary<string, IList<string>>();
            foreach (string val in vals)
            {
                ComboSelection sel = ComboSelection.Parse(val, sep);
                if (sel != null)
                {
                    IList<string> valList = retmap.Get(sel.Name);
                    if (valList == null)
                    {
                        valList = new List<string>();
                        retmap.Put(sel.Name, valList);
                    }
                    valList.Add(sel.Value);
                }
            }
            return retmap;
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            IDictionary<string, IList<string>> valMap = ConvertMap(vals, _separator);

            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            foreach (var entry in valMap)
            {
                string name = entry.Key;
                IFacetHandler facetHandler = GetDependedFacetHandler(name);
                if (facetHandler == null)
                {
                    return EmptyFilter.Instance;
                }
                IList<string> selVals = entry.Value;
                if (selVals == null || selVals.Count == 0) return EmptyFilter.Instance;
                RandomAccessFilter f = facetHandler.BuildRandomAccessAndFilter(selVals.ToArray(), prop);
                if (f == EmptyFilter.Instance) return f;
                filterList.Add(f);
            }

            if (filterList.Count == 0)
            {
                return EmptyFilter.Instance;
            }
            if (filterList.Count == 1)
            {
                return filterList.Get(0);
            }
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            IDictionary<string, IList<string>> valMap = ConvertMap(vals, _separator);

            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            foreach (var entry in valMap)
            {
                string name = entry.Key;
                IFacetHandler facetHandler = GetDependedFacetHandler(name);
                if (facetHandler == null)
                {
                    continue;
                }
                IList<string> selVals = entry.Value;
                if (selVals == null || selVals.Count == 0)
                {
                    continue;
                }
                RandomAccessFilter f = facetHandler.BuildRandomAccessOrFilter(selVals.ToArray(), prop, isNot);
                if (f == EmptyFilter.Instance) continue;
                filterList.Add(f);
            }

            if (filterList.Count == 0)
            {
                return EmptyFilter.Instance;
            }
            if (filterList.Count == 1)
            {
                return filterList.Get(0);
            }

            if (isNot)
            {
                return new RandomAccessAndFilter(filterList);
            }
            else
            {
                return new RandomAccessOrFilter(filterList);
            }
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            throw new NotSupportedException("sorting not supported for " + typeof(ComboFacetHandler));
        }

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            throw new NotSupportedException("facet counting not supported for " + typeof(ComboFacetHandler));
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            IEnumerable<string> dependsOn = this.DependsOn;
            List<string> valueList = new List<string>();
            foreach (string depends in dependsOn)
            {
                IFacetHandler facetHandler = GetDependedFacetHandler(depends);
                string[] fieldValues = facetHandler.GetFieldValues(reader, id);
                foreach (string fieldVal in fieldValues)
                {
                    StringBuilder buf = new StringBuilder();
                    buf.Append(depends).Append(_separator).Append(fieldVal);
                    valueList.Add(buf.ToString());
                }
            }
            return valueList.ToArray();
        }

        public override int GetNumItems(BoboIndexReader reader, int id)
        {
            IEnumerable<string> dependsOn = this.DependsOn;
            List<string> valueList = new List<string>();
            int count = 0;
            foreach (string depends in dependsOn)
            {
                IFacetHandler facetHandler = GetDependedFacetHandler(depends);
                string[] fieldValues = facetHandler.GetFieldValues(reader, id);
                if (fieldValues != null)
                {
                    count++;
                }
            }
            return count;
        }

        public override FacetDataNone Load(BoboIndexReader reader)
        {
            return FacetDataNone.Instance;
        }
    }
}
