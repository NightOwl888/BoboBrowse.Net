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
namespace BoboBrowse.Net.Facets.Filter
{
    using BoboBrowse.Net.DocIdSet;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System.Collections.Generic;
    using System.Linq;

    public class OrFilter : Filter
    {
        private readonly IList<Filter> m_filters;

        public OrFilter(IList<Filter> filters)
        {
            m_filters = filters;
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            var count = m_filters.Count;
            if (count == 1)
            {
                return m_filters[0].GetDocIdSet(context, acceptDocs);
            }
            else
            {
                List<DocIdSet> list = new List<DocIdSet>(count);
                foreach (Filter f in m_filters)
                {
                    list.Add(f.GetDocIdSet(context, acceptDocs));
                }
                return new OrDocIdSet(list);
            }
        }
    }
}