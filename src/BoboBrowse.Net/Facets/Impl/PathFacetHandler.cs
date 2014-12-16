//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    public class PathFacetHandler : FacetHandler<FacetDataCache>
    {
        private const string DEFAULT_SEP = "/";

        public const string SEL_PROP_NAME_STRICT = "strict";
        public const string SEL_PROP_NAME_DEPTH = "depth";

        private readonly bool _multiValue;

        private readonly TermListFactory _termListFactory;
        private string _separator;
        private readonly string _indexedName;

        public PathFacetHandler(string name)
            : this(name, false)
        {
        }

        public PathFacetHandler(string name, bool multiValue)
            : this(name, name, multiValue)
        {
        }

        public PathFacetHandler(string name, string indexedName, bool multiValue)
            : base(name)
        {
            _indexedName = indexedName;
            _multiValue = multiValue;
            _termListFactory = TermListFactory.StringListFactory;
            _separator = DEFAULT_SEP;
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

        public override int GetNumItems(BoboIndexReader reader, int id)
        {
            IFacetDataCache data = GetFacetData(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
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

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new FacetDataCache.FacetDocComparatorSource(this);
        }

        public override string[] GetFieldValues(BoboIndexReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData(reader);
            if (dataCache == null) return new string[0];
            if (_multiValue)
            {
                return ((MultiValueFacetDataCache)dataCache).NestedArray.GetTranslatedData(id, dataCache.valArray);
            }
            else
            {

                return new string[] { dataCache.ValArray.get(dataCache.OrderArray.Get(id)) };
            }
        }

        public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
        {
            return GetFieldValues(reader, id);
        }

        public virtual string Separator
        {
            get { return _separator; }
            set { _separator = value; }
        }

        private class PathValueConverter : IFacetValueConverter
        {
            private readonly bool _strict;
            private readonly string _sep;
            private readonly int _depth;
            public PathValueConverter(int depth, bool strict, string sep)
            {
                _strict = strict;
                _sep = sep;
                _depth = depth;
            }

            private void GetFilters(IFacetDataCache dataCache, IList<int> intSet, string[] vals, int depth, bool strict)
            {
                foreach (string val in vals)
                {
                    GetFilters(dataCache, intSet, val, depth, strict);
                }
            }

            private void GetFilters(IFacetDataCache dataCache, IList<int> intSet, string val, int depth, bool strict)
            {
                IList<string> termList = dataCache.ValArray;
                int index = termList.IndexOf(val);

                int startDepth = GetPathDepth(val, _sep);

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
                    string path = termList[i];
                    if (path.StartsWith(val))
                    {
                        if (!strict || GetPathDepth(path, _sep) - startDepth == depth)
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

            public int[] Convert(IFacetDataCache dataCache, string[] vals)
            {
                IList<int> intSet = new List<int>();
                GetFilters(dataCache, intSet, vals, _depth, _strict);
                return intSet.ToArray();
            }

            // NOTE: Originally, this method was in the PathFacetHandler class, but was moved here
            // because this is the only class that references it.
            private int GetPathDepth(string path, string separator)
            {
                return path.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, Properties props)
        {
            int depth = GetDepth(props);
            bool strict = IsStrict(props);
            PathValueConverter valConverter = new PathValueConverter(depth, strict, _separator);
            string[] vals = new string[] { value };

            return _multiValue ? new MultiValueORFacetFilter(this, vals, valConverter, false) : new FacetOrFilter(this, vals, false, valConverter);
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
                if (vals.Length > 0)
                {
                    int depth = GetDepth(prop);
                    bool strict = IsStrict(prop);
                    PathValueConverter valConverter = new PathValueConverter(depth, strict, _separator);
                    return _multiValue ? new MultiValueORFacetFilter(this, vals, valConverter, isNot) : new FacetOrFilter(this, vals, isNot, valConverter);
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

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec)
        {
            return new PathFacetHandlerFacetCountCollectorSource(this, _name, _separator, sel, fspec, _multiValue);
        }

        public class PathFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly PathFacetHandler _parent;
            private readonly string _name;
            private readonly string _separator;
            private readonly BrowseSelection _sel;
            private readonly FacetSpec _ospec;
            private readonly bool _multiValue;

            public PathFacetHandlerFacetCountCollectorSource(PathFacetHandler parent, string name, string separator, BrowseSelection sel, FacetSpec ospec, bool multiValue)
            {
                _parent = parent;
                _name = name;
                _separator = separator;
                _sel = sel;
                _ospec = ospec;
                _multiValue = multiValue;
            }

            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                IFacetDataCache dataCache = _parent.GetFacetData(reader);
				if (_multiValue)
                {
					return new MultiValuedPathFacetCountCollector(_name, _separator, _sel, _ospec, dataCache);
				}
				else
                {
					return new PathFacetCountCollector(_name, _separator, _sel, _ospec, dataCache);
				}
            }
        }

        // TODO: Possibly need to create a factory of some kind to create the
        // cache instances.
        public override IFacetDataCache Load(BoboIndexReader reader)
        {
            if (!_multiValue)
            {
                IFacetDataCache dataCache = new FacetDataCache();
                dataCache.Load(_indexedName, reader, _termListFactory);
                return dataCache;
            }
            else
            {
                MultiValueFacetDataCache dataCache = new MultiValueFacetDataCache();
                dataCache.Load(_indexedName, reader, _termListFactory);
                return dataCache;
            }
        }
    }
}
