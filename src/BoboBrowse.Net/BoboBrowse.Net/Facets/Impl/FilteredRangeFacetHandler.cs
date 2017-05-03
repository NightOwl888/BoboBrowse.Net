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
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using System.Collections.Generic;
    using System.IO;

    public class FilteredRangeFacetHandler : FacetHandler<FacetDataNone>
	{
        private readonly IList<string> m_predefinedRanges;
		private readonly string m_inner;
		private RangeFacetHandler m_innerHandler;

        public FilteredRangeFacetHandler(string name, string underlyingHandler, IList<string> predefinedRanges)
            : base(name, new string[] { underlyingHandler })
        {
            m_predefinedRanges = predefinedRanges;
            m_inner = underlyingHandler;
            m_innerHandler = null;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty)
		{
			return m_innerHandler.BuildRandomAccessFilter(value, selectionProperty);
		}


        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
		{
			return m_innerHandler.BuildRandomAccessAndFilter(vals, prop);
		}

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
		{
			return m_innerHandler.BuildRandomAccessOrFilter(vals, prop, isNot);
		}

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec) 
        {
            return new FilteredRangeFacetCountCollectorSource(m_innerHandler, m_name, fspec, m_predefinedRanges);
		}

        private class FilteredRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly RangeFacetHandler m_innerHandler;
            private readonly string m_name;
            private readonly FacetSpec m_fspec;
            private readonly IList<string> m_predefinedRanges;

            public FilteredRangeFacetCountCollectorSource(RangeFacetHandler innerHandler, string name, FacetSpec fspec, IList<string> predefinedRanges)
            {
                this.m_innerHandler = innerHandler;
                this.m_name = name;
                this.m_fspec = fspec;
                this.m_predefinedRanges = predefinedRanges;
            }
            public override IFacetCountCollector GetFacetCountCollector(BoboSegmentReader reader, int docBase)
            {
                FacetDataCache dataCache = m_innerHandler.GetFacetData<FacetDataCache>(reader);
                return new RangeFacetCountCollector(m_name, dataCache, docBase, m_fspec, m_predefinedRanges);
            }
        }

        public override string[] GetFieldValues(BoboSegmentReader reader, int id)
		{
			return m_innerHandler.GetFieldValues(reader, id);
		}

        public override object[] GetRawFieldValues(BoboSegmentReader reader, int id)
		{
			return m_innerHandler.GetRawFieldValues(reader, id);
		}

        public override DocComparerSource GetDocComparerSource()
        {
            return m_innerHandler.GetDocComparerSource();
        }

        public override FacetDataNone Load(BoboSegmentReader reader)
		{
			IFacetHandler handler = reader.GetFacetHandler(m_inner);
			if (handler is RangeFacetHandler)
			{
				m_innerHandler = (RangeFacetHandler)handler;
                return FacetDataNone.Instance;
			}
			else
			{
                throw new IOException("inner handler is not instance of RangeFacetHandler");
			}
		}
	}
}