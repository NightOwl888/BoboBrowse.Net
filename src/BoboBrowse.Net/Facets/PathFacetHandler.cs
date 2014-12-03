//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Util;

    public class PathFacetHandler : FacetHandler, IFacetHandlerFactory
    {
        private const string DEFAULT_SEP = "/";

        public const string SEL_PROP_NAME_STRICT = "strict";
        public const string SEL_PROP_NAME_DEPTH = "depth";

        private FacetDataCache dataCache;
        private readonly TermListFactory termListFactory;
        private string separator;

        public PathFacetHandler(string name)
            : base(name)
        {
            dataCache = null;
            termListFactory = TermListFactory.StringListFactory;
            separator = DEFAULT_SEP;
        }

        public virtual FacetHandler NewInstance()
        {
            return new PathFacetHandler(Name);
        }

        public FacetDataCache GetDataCache()
        {
            return dataCache;
        }

        ///<summary>Sets is strict applied for counting. Used if the field is of type <b><i>path</i></b>. </summary>
        ///<param name="strict"> is strict applied </param>
        public static void SetStrict(Properties props, bool strict)
        {
            props.SetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT, Convert.ToString(strict));
        }


        ///<summary>Sets the depth.  Used if the field is of type <b><i>path</i></b>. </summary>
        ///<param name="depth">depth </param>
        public static void SetDepth(Properties props, int depth)
        {
            props.SetProperty(PathFacetHandler.SEL_PROP_NAME_DEPTH, Convert.ToString(depth));
        }


        ///<summary> Gets if strict applied for counting. Used if the field is of type <b><i>path</i></b>. </summary>
        ///<returns> is strict applied </returns>
        public static bool IsStrict(Properties selectionProp)
        {
            try
            {
                return Convert.ToBoolean(selectionProp.GetProperty(PathFacetHandler.SEL_PROP_NAME_STRICT));
            }
            catch
            {
                return false;
            }
        }

        ///<summary> Gets the depth.  Used if the field is of type <b><i>path</i></b>. </summary>
        ///<returns> depth </returns>
        public static int GetDepth(Properties selectionProp)
        {
            try
            {
                return Convert.ToInt32(selectionProp.GetProperty(PathFacetHandler.SEL_PROP_NAME_DEPTH));
            }
            catch
            {
                return 1;
            }
        }

        public override FieldComparator GetComparator(int numDocs, SortField field)
        {
            return dataCache.GeFieldComparator(numDocs, field.Type);
        }

        public override string[] GetFieldValues(int id)
        {
            return new string[] { dataCache.valArray.Get(dataCache.orderArray.Get(id)) };
        }

        public override object[] GetRawFieldValues(int id)
        {
            return GetFieldValues(id);
        }

        public virtual void SetSeparator(string separator)
        {
            this.separator = separator;
        }

        public virtual string GetSeparator()
        {
            return separator;
        }

        private int GetPathDepth(string path)
        {
            return path.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private void GetFilters(List<int> intSet, string[] vals, int depth, bool strict)
        {
            foreach (string val in vals)
            {
                GetFilters(intSet, val, depth, strict);
            }
        }

        private void GetFilters(List<int> intSet, string val, int depth, bool strict)
        {
            ITermValueList termList = dataCache.valArray;
            int index = termList.IndexOf(val);

            int startDepth = GetPathDepth(val);

            if (index < 0)
            {
                int nextIndex = -(index + 1);
                if (nextIndex == termList.Count)
                {
                    return;
                }
                index = nextIndex;
            }


            for (int i = index; i < termList.Count; ++i)
            {
                string path = termList.Get(i);
                if (path.StartsWith(val))
                {
                    if (!strict || GetPathDepth(path) - startDepth == depth)
                    {
                        intSet.Add(i);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties props)
        {
            int depth = GetDepth(props);
            bool strict = IsStrict(props);
            List<int> intSet = new List<int>();
            GetFilters(intSet, @value, depth, strict);
            if (intSet.Count > 0)
            {
                int[] indexes = intSet.ToArray();
                return new FacetOrFilter(dataCache, indexes);
            }
            else
            {
                return null;
            }
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            if (vals.Length > 1)
            {
                return EmptyFilter.GetInstance();
            }
            else
            {
                RandomAccessFilter f = BuildRandomAccessFilter(vals[0], prop);
                if (f != null)
                {
                    return f;
                }
                else
                {
                    return EmptyFilter.GetInstance();
                }
            }
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            if (vals.Length > 1)
            {
                int depth = GetDepth(prop);
                bool strict = IsStrict(prop);
                List<int> intSet = new List<int>();
                GetFilters(intSet, vals, depth, strict);
                if (intSet.Count > 0)
                {
                    return new FacetOrFilter(dataCache, intSet.ToArray(), isNot);
                }
                else
                {
                    if (isNot)
                    {
                        return null;
                    }
                    else
                    {
                        return EmptyFilter.GetInstance();
                    }
                }
            }
            else
            {
                RandomAccessFilter f = BuildRandomAccessFilter(vals[0], prop);
                if (f == null)
                    return f;
                if (isNot)
                {
                    f = new RandomAccessNotFilter(f);
                }
                return f;
            }
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec ospec)
        {
            return new PathFacetCountCollector(this.Name, separator, sel, ospec, dataCache);
        }

        public override void Load(BoboIndexReader reader)
        {
            if (dataCache == null)
            {
                dataCache = new FacetDataCache();
            }
            dataCache.Load(this.Name, reader, termListFactory);
        }

        private sealed class PathFacetCountCollector : IFacetCountCollector
        {
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;
            private int[] _count;
            private readonly string _name;
            private readonly string _sep;
            private readonly BigSegmentedArray _orderArray;
            private readonly FacetDataCache _dataCache;

            internal PathFacetCountCollector(string name, string sep, BrowseSelection sel, FacetSpec ospec, FacetDataCache dataCache)
            {
                _sel = sel;
                _ospec = ospec;
                _name = name;
                _dataCache = dataCache;
                _sep = sep;
                _count = new int[_dataCache.freqs.Length];
                _orderArray = _dataCache.orderArray;
            }

            public int[] GetCountDistribution()
            {
                return _count;
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            public void Collect(int docid)
            {
                _count[_orderArray.Get(docid)]++;
            }

            public void CollectAll()
            {
                _count = _dataCache.freqs;
            }
            public BrowseFacet GetFacet(string @value)
            {
                return null;
            }

            private IEnumerable<BrowseFacet> getFacetsForPath(string selectedPath, int depth, bool strict, int minCount)
            {
                LinkedList<BrowseFacet> list = new LinkedList<BrowseFacet>();

                string[] startParts = null;
                int startDepth = 0;

                if (selectedPath != null && selectedPath.Length > 0)
                {
                    startParts = selectedPath.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                    startDepth = startParts.Length;
                    if (!selectedPath.EndsWith(_sep))
                    {
                        selectedPath += _sep;
                    }
                }

                string currentPath = null;
                int currentCount = 0;

                int wantedDepth = startDepth + depth;

                int index = 0;
                if (selectedPath != null && selectedPath.Length > 0)
                {
                    index = _dataCache.valArray.IndexOf(selectedPath);
                    if (index < 0)
                    {
                        index = -(index + 1);
                    }
                }

                for (int i = index; i < _count.Length; ++i)
                {
                    if (_count[i] >= minCount)
                    {
                        string path = _dataCache.valArray.Get(i);
                        //if (path==null || path.equals(selectedPath)) continue;						

                        int subCount = _count[i];

                        string[] pathParts = path.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);

                        int pathDepth = pathParts.Length;

                        if ((startDepth == 0) || (startDepth > 0 && path.StartsWith(selectedPath)))
                        {
                            StringBuilder buf = new StringBuilder();
                            int minDepth = Math.Min(wantedDepth, pathDepth);
                            for (int k = 0; k < minDepth; ++k)
                            {
                                buf.Append(pathParts[k]);
                                if (!pathParts[k].EndsWith(_sep))
                                {
                                    if (pathDepth != wantedDepth || k < (wantedDepth - 1))
                                        buf.Append(_sep);
                                }
                            }
                            string wantedPath = buf.ToString();
                            if (currentPath == null)
                            {
                                currentPath = wantedPath;
                                currentCount = subCount;
                            }
                            else if (wantedPath.Equals(currentPath))
                            {
                                if (!strict)
                                {
                                    currentCount += subCount;
                                }
                            }
                            else
                            {
                                bool directNode = false;

                                if (wantedPath.EndsWith(_sep))
                                {
                                    if (currentPath.Equals(wantedPath.Substring(0, wantedPath.Length - 1)))
                                    {
                                        directNode = true;
                                    }
                                }

                                if (strict)
                                {
                                    if (directNode)
                                    {
                                        currentCount += subCount;
                                    }
                                    else
                                    {
                                        BrowseFacet ch = new BrowseFacet(currentPath, currentCount);
                                        list.AddLast(ch);
                                        currentPath = wantedPath;
                                        currentCount = subCount;
                                    }
                                }
                                else
                                {
                                    if (!directNode)
                                    {
                                        BrowseFacet ch = new BrowseFacet(currentPath, currentCount);
                                        list.AddLast(ch);
                                        currentPath = wantedPath;
                                        currentCount = subCount;
                                    }
                                    else
                                    {
                                        currentCount += subCount;
                                    }
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (currentPath != null && currentCount > 0)
                {
                    list.AddLast(new BrowseFacet(currentPath, currentCount));
                }

                return list;
            }

            public IEnumerable<BrowseFacet> GetFacets()
            {
                int minCount = _ospec.MinHitCount;

                Properties props = _sel == null ? null : _sel.SelectionProperties;
                int depth = PathFacetHandler.GetDepth(props);
                bool strict = PathFacetHandler.IsStrict(props);

                string[] paths = _sel == null ? null : _sel.Values;
                if (paths == null || paths.Length == 0)
                {
                    return getFacetsForPath(null, depth, strict, minCount);
                }

                List<BrowseFacet> finalList = new List<BrowseFacet>();
                foreach (string path in paths)
                {
                    IEnumerable<BrowseFacet> subList = getFacetsForPath(path, depth, strict, minCount);
                    finalList.AddRange(subList);
                }
                return finalList;
            }
        }
    }
}
