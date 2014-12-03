//* 
//* Copyright (C) 2005-2006  John Wang
//*
//* This library is free software; you can redistribute it and/or
//* modify it under the terms of the GNU Lesser General Public
//* License as published by the Free Software Foundation; either
//* version 2.1 of the License, or (at your option) any later version.
//*
//* This library is distributed in the hope that it will be useful,
//* but WITHOUT ANY WARRANTY; without even the implied warranty of
//* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//* Lesser General Public License for more details.
//*
//* You should have received a copy of the GNU Lesser General Public
//* License along with this library; if not, write to the Free Software
//* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//* 
//* To contact the project administrators for the bobo-browse project, 
//* please go to https://sourceforge.net/projects/bobo-browse/, or 
//* send mail to owner@browseengine.com. 

namespace BoboBrowse.Net.Facets.Impl
{
    using System;    
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Common.Logging;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Search;

    public class RangeFacetHandler : FacetHandler, IFacetHandlerFactory
    {
        private static ILog logger = LogManager.GetLogger(typeof(RangeFacetHandler));

        private FacetDataCache dataCache;
        private readonly string indexFieldName;
        private readonly TermListFactory termListFactory;
        private readonly List<string> predefinedRanges;
        private readonly bool autoRange;

        public RangeFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, List<string> predefinedRanges)
            : base(name)
        {
            this.indexFieldName = indexFieldName;
            this.dataCache = null;
            this.termListFactory = termListFactory;
            this.predefinedRanges = predefinedRanges;
            this.autoRange = false;
        }

        public RangeFacetHandler(string name, TermListFactory termListFactory, List<string> predefinedRanges)
            : this(name, name, termListFactory, predefinedRanges)
        {
        }

        public RangeFacetHandler(string name, List<string> predefinedRanges)
            : this(name, name, null, predefinedRanges)
        {
        }

        public RangeFacetHandler(string name, string indexFieldName, List<string> predefinedRanges)
            : this(name, indexFieldName, null, predefinedRanges)
        {
        }

        public RangeFacetHandler(string name, string indexFieldName, TermListFactory termListFactory, bool autoRange)
            : base(name)
        {
            this.dataCache = null;
            this.indexFieldName = indexFieldName;
            this.termListFactory = termListFactory;
            this.predefinedRanges = null;
            this.autoRange = autoRange;
        }

        public RangeFacetHandler(string name, TermListFactory termListFactory, bool autoRange)
            : this(name, name, termListFactory, autoRange)
        {
        }

        public RangeFacetHandler(string name, string indexFieldName, bool autoRange)
            : this(name, indexFieldName, null, autoRange)
        {
        }

        public RangeFacetHandler(string name, bool autoRange)
            : this(name, name, null, autoRange)
        {
        }

        public virtual FacetHandler NewInstance()
        {
            if (predefinedRanges == null)
            {
                return new RangeFacetHandler(Name, indexFieldName, termListFactory, autoRange);
            }
            else
            {
                return new RangeFacetHandler(Name, indexFieldName,termListFactory, predefinedRanges);
            }
        }

        public virtual bool IsAutoRange()
        {
            return autoRange;
        }

        public override FieldComparator GetComparator(int numDocs,SortField field)
        {
            return dataCache.GeFieldComparator(numDocs, field.Type);
        }

        public override string[] GetFieldValues(int id)
        {
            return new string[] { dataCache.valArray.Get(dataCache.orderArray.Get(id)) };
        }

        public override object[] GetRawFieldValues(int id)
        {
            return new object[] { dataCache.valArray.GetRawValue(dataCache.orderArray.Get(id)) };
        }

        public static string[] GetRangeStrings(string rangeString)
        {
            int index = rangeString.IndexOf('[');
            int index2 = rangeString.IndexOf(" TO ", StringComparison.InvariantCultureIgnoreCase);
            int index3 = rangeString.LastIndexOf(']');

            if (index == -1 || index2 == -1 || index3 == -1)
            {
                return new string[] { rangeString, rangeString };
            }
            string lower, upper;

            lower = rangeString.Substring(index + 1, index2 - index - 1).Trim();
            upper = rangeString.Substring(index2 + 4, index3 - index2 - 4).Trim();

            return new string[] { lower, upper };
        }

        internal static int[] Parse(FacetDataCache dataCache, string rangeString)
        {
            string[] ranges = GetRangeStrings(rangeString);
            string lower = ranges[0];
            string upper = ranges[1];

            if ("*".Equals(lower) || "".Equals(lower))
            {
                lower = null;
            }

            if ("*".Equals(upper) || "".Equals(upper))
            {
                upper = null;
            }

            int start, end;
            if (lower == null)
            {
                start = 0;
            }
            else
            {
                start = dataCache.valArray.IndexOf(lower);
                if (start < 0)
                {
                    start = -(start + 1);
                }
            }

            if (upper == null)
            {
                end = dataCache.valArray.Count - 1;
            }
            else
            {
                end = dataCache.valArray.IndexOf(upper);
                if (end < 0)
                {
                    end = -(end + 1);
                    end = Math.Max(0, end - 1);
                }
            }

            return new int[] { start, end };
        }

        public FacetDataCache GetDataCache()
        {
            return dataCache;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties prop)
        {
            int[] range = Parse(dataCache, @value);
            if (range != null)
                return new FacetRangeFilter(dataCache, range[0], range[1]);
            else
                return null;
        }

        public static int[] ConvertIndexes(FacetDataCache dataCache, string[] vals)
        {
            List<int> list = new List<int>();
            foreach (string val in vals)
            {
                int[] range = Parse(dataCache, val);
                if (range != null)
                {
                    for (int i = range[0]; i <= range[1]; ++i)
                    {
                        list.Add(i);
                    }
                }
            }
            return list.ToArray();
        }

        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, Properties prop)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>(vals.Length);

            foreach (string val in vals)
            {
                RandomAccessFilter f = BuildRandomAccessFilter(val, prop);
                if (f != null)
                {
                    filterList.Add(f);
                }
                else
                {
                    return EmptyFilter.GetInstance();
                }
            }

            if (filterList.Count == 1)
                return filterList[0];
            return new RandomAccessAndFilter(filterList);
        }

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, Properties prop, bool isNot)
        {
            if (vals.Length > 1)
            {
                return new FacetOrFilter(dataCache, ConvertIndexes(dataCache, vals), isNot);
            }
            else
            {
                RandomAccessFilter filter = BuildRandomAccessFilter(vals[0], prop);
                if (filter == null)
                    return filter;
                if (isNot)
                {
                    filter = new RandomAccessNotFilter(filter);
                }
                return filter;
            }
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec ospec)
        {
            return new RangeFacetCountCollector(this.Name, dataCache, ospec, predefinedRanges, autoRange);
        }

        public override void Load(BoboIndexReader reader)
        {
            if (dataCache == null)
            {
                dataCache = new FacetDataCache();
            }
            dataCache.Load(indexFieldName, reader, termListFactory);
        }

        public override IFacetAccessible Merge(FacetSpec fspec, List<IFacetAccessible> facetList)
        {
            if (autoRange)
            {
                throw new InvalidOperationException("Cannot support merging for autoRange");
            }
            return base.Merge(fspec, facetList);
        }
    }
}
