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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Data;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Sort;
    using BoboBrowse.Net.Support;
    using System.Collections.Generic;
    using System.IO;

    public class FilteredRangeFacetHandler : FacetHandler<FacetDataNone>
	{
        private readonly IEnumerable<string> _predefinedRanges;
		private readonly string _inner;
		private RangeFacetHandler _innerHandler;

        public FilteredRangeFacetHandler(string name, string underlyingHandler, IEnumerable<string> predefinedRanges)
            : base(name, new string[] { underlyingHandler })
        {
            _predefinedRanges = predefinedRanges;
            _inner = underlyingHandler;
            _innerHandler = null;
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string value, IDictionary<string, string> selectionProperty)
		{
			return _innerHandler.BuildRandomAccessFilter(value, selectionProperty);
		}


        public override RandomAccessFilter BuildRandomAccessAndFilter(string[] vals, IDictionary<string, string> prop)
		{
			return _innerHandler.BuildRandomAccessAndFilter(vals, prop);
		}

        public override RandomAccessFilter BuildRandomAccessOrFilter(string[] vals, IDictionary<string, string> prop, bool isNot)
		{
			return _innerHandler.BuildRandomAccessOrFilter(vals, prop, isNot);
		}

        public override FacetCountCollectorSource GetFacetCountCollectorSource(BrowseSelection sel, FacetSpec fspec) 
        {
            return new FilteredRangeFacetCountCollectorSource(_innerHandler, _name, fspec, _predefinedRanges);
		}

        private class FilteredRangeFacetCountCollectorSource : FacetCountCollectorSource
        {
            private readonly RangeFacetHandler _innerHandler;
            private readonly string _name;
            private readonly FacetSpec _fspec;
            private readonly IEnumerable<string> _predefinedRanges;

            public FilteredRangeFacetCountCollectorSource(RangeFacetHandler innerHandler, string name, FacetSpec fspec, IEnumerable<string> predefinedRanges)
            {
                this._innerHandler = innerHandler;
                this._name = name;
                this._fspec = fspec;
                this._predefinedRanges = predefinedRanges;
            }
            public override IFacetCountCollector GetFacetCountCollector(BoboIndexReader reader, int docBase)
            {
                FacetDataCache dataCache = _innerHandler.GetFacetData<FacetDataCache>(reader);
                return new RangeFacetCountCollector(_name, dataCache, docBase, _fspec, _predefinedRanges);
            }
        }

		public override string[] GetFieldValues(BoboIndexReader reader, int id)
		{
			return _innerHandler.GetFieldValues(reader, id);
		}

		public override object[] GetRawFieldValues(BoboIndexReader reader, int id)
		{
			return _innerHandler.GetRawFieldValues(reader, id);
		}

        public override DocComparatorSource GetDocComparatorSource()
        {
            return _innerHandler.GetDocComparatorSource();
        }

		public override FacetDataNone Load(BoboIndexReader reader)
		{
			IFacetHandler handler = reader.GetFacetHandler(_inner);
			if (handler is RangeFacetHandler)
			{
				_innerHandler = (RangeFacetHandler)handler;
                return FacetDataNone.Instance;
			}
			else
			{
                throw new IOException("inner handler is not instance of RangeFacetHandler");
			}
		}
	}
}