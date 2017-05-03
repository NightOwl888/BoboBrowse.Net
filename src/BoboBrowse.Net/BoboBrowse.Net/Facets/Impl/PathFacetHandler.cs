//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
//*
//* Licensed under the Apache License, Version 2.0 (the "License");
//* you may not use this file except in compliance with the License.
//* You may obtain a copy of the License at
//*
//*   http://www.apache.org/licenses/LICENSE-2.0
//*
//* Unless required by applicable law or agreed to in writing, software
//* distributed under the License is distributed on an "AS IS" BASIS,
//* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//* See the License for the specific language governing permissions and
//* limitations under the License.

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Used to denote facet values with hierarchical structure, for example: "A/B/C/D". 
    /// Multiple values in this field are allowed if constructor parameter multiValue is set to true. 
    /// When being indexed, this field should not be tokenized.
    /// </summary>
    public class PathFacetHandler : FacetHandler<FacetDataCache>
    {
        private const string DEFAULT_SEP = "/";

        public const string SEL_PROP_NAME_STRICT = "strict";
        public const string SEL_PROP_NAME_DEPTH = "depth";

        private readonly bool _multiValue;

        private readonly TermListFactory _termListFactory;
        private string _separator;
        private readonly string _indexedName;

        /// <summary>
        /// Initializes a new instance of <see cref="T:PathFacetHandler"/> with the specified name.
        /// The Lucene.Net index field must have the same name. The field separator is assumed to be "/".
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        public PathFacetHandler(string name)
            : this(name, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PathFacetHandler"/> with the specified name.
        /// The Lucene.Net index field must have the same name. The field separator is assumed to be "/".
        /// </summary>
        /// <param name="name">The facet handler name. Must be the same value as the Lucene.Net index field name.</param>
        /// <param name="multiValue">Indicates whether multiple values are allowed in this field.</param>
        public PathFacetHandler(string name, bool multiValue)
            : this(name, name, multiValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PathFacetHandler"/> with the specified name and 
        /// Lucene.Net index field name. The field separator is assumed to be "/"
        /// </summary>
        /// <param name="name">The name of the facet handler.</param>
        /// <param name="indexedName">The name of the Lucene.Net index field this handler will utilize.</param>
        /// <param name="multiValue">Indicates whether multiple values are allowed in this field.</param>
        public PathFacetHandler(string name, string indexedName, bool multiValue)
            : base(name)
        {
            _indexedName = indexedName;
            _multiValue = multiValue;
            _termListFactory = TermListFactory.StringListFactory;
            _separator = DEFAULT_SEP;
        }

        ///<summary>Sets is strict applied for counting. Used if the field is of type <b><i>path</i></b>. </summary>
        ///<param name="props">The properties dictionary to set the property value on.</param>
        ///<param name="strict"> is strict applied </param>
        public static void SetStrict(IDictionary<string, string> props, bool strict)
        {
            props.Put(PathFacetHandler.SEL_PROP_NAME_STRICT, Convert.ToString(strict));
        }

        ///<summary>Sets the depth.  Used if the field is of type <b><i>path</i></b>. </summary>
        ///<param name="props">The properties dictionary to set the property value on.</param>
        ///<param name="depth">depth </param>
        public static void SetDepth(IDictionary<string, string> props, int depth)
        {
            props.Put(PathFacetHandler.SEL_PROP_NAME_DEPTH, Convert.ToString(depth));
        }

        ///<summary> Gets if strict applied for counting. Used if the field is of type <b><i>path</i></b>. </summary>
        ///<returns> is strict applied </returns>
        public static bool IsStrict(IDictionary<string, string> selectionProp)
        {
            try
            {
                return Convert.ToBoolean(selectionProp.Get(PathFacetHandler.SEL_PROP_NAME_STRICT));
            }
            catch
            {
                return false;
            }
        }

        public override int GetNumItems(BoboSegmentReader reader, int id)
        {
            FacetDataCache data = GetFacetData<FacetDataCache>(reader);
            if (data == null) return 0;
            return data.GetNumItems(id);
        }

        ///<summary> Gets the depth.  Used if the field is of type <b><i>path</i></b>. </summary>
        ///<returns> depth </returns>
        public static int GetDepth(IDictionary<string, string> selectionProp)
        {
            try
            {
                return Convert.ToInt32(selectionProp.Get(PathFacetHandler.SEL_PROP_NAME_DEPTH));
            }
            catch
            {
                return 1;
            }
        }

        public override DocComparatorSource GetDocComparatorSource()
        {
            return new FacetDocComparatorSource(this);
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
        {
            FacetDataCache dataCache = GetFacetData<FacetDataCache>(reader);
            if (dataCache == null) return new string[0];
            if (_multiValue)
            {
                return ((MultiValueFacetDataCache)dataCache).NestedArray.GetTranslatedData(id, dataCache.ValArray);
            }
            else
            {

                return new string[] { dataCache.ValArray.Get(dataCache.OrderArray.Get(id)) };
            }
        }

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
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

            private void GetFilters(FacetDataCache dataCache, IList<int> intSet, string[] vals, int depth, bool strict)
            {
                foreach (string val in vals)
                {
                    GetFilters(dataCache, intSet, val, depth, strict);
                }
            }

            private void GetFilters(FacetDataCache dataCache, IList<int> intSet, string val, int depth, bool strict)
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

            public virtual int[] Convert(FacetDataCache dataCache, string[] vals)
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

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> props)
        {
            int depth = GetDepth(props);
            bool strict = IsStrict(props);
            PathValueConverter valConverter = new PathValueConverter(depth, strict, _separator);
            string[] vals = new string[] { value };

            if (_multiValue)
            {
                return new MultiValueORFacetFilter(this, vals, valConverter, false);
            }
            return new FacetOrFilter(this, vals, false, valConverter);

            //return _multiValue ? new MultiValueORFacetFilter(this, vals, valConverter, false) : new FacetOrFilter(this, vals, false, valConverter);
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
        {
            if (vals.Length > 1)
            {
                return EmptyFilter.Instance;
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
                    return EmptyFilter.Instance;
                }
            }
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
        {
            if (vals.Length > 1)
            {
                if (vals.Length > 0)
                {
                    int depth = GetDepth(prop);
                    bool strict = IsStrict(prop);
                    PathValueConverter valConverter = new PathValueConverter(depth, strict, _separator);

                    if (_multiValue)
                    {
                        return new MultiValueORFacetFilter(this, vals, valConverter, isNot);
                    }
                    return new FacetOrFilter(this, vals, isNot, valConverter);
                }
                else
                {
                    if (isNot)
                    {
                        return null;
                    }
                    else
                    {
                        return EmptyFilter.Instance;
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

        private class PathFacetHandlerFacetCountCollectorSource : FacetCountCollectorSource
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

            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = _parent.GetFacetData<FacetDataCache>(reader);
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

        public override FacetDataCache Load(BoboSegmentReader reader)
        {
            if (!_multiValue)
            {
                FacetDataCache dataCache = new FacetDataCache();
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
