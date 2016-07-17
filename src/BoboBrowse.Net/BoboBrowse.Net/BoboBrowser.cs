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
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using System.Collections.Generic;
    using System.Linq;

    ///<summary>
    /// This class implements the browsing functionality.
    /// author ymatsuda
    ///</summary>
    public class BoboBrowser : MultiBoboBrowser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:BoboBrowser"/> class with the specified <see cref="T:BoboIndexReader"/> instance.
        /// </summary>
        /// <param name="reader">An open <see cref="T:BoboIndexReader"/> instance.</param>
        public BoboBrowser(BoboMultiReader reader)
            : base(CreateBrowsables(reader.SubReaders))
        {}

        public static IList<BoboSegmentReader> GatherSubReaders(IList<BoboMultiReader> readerList)
        {
            IList<BoboSegmentReader> subReaderList = new List<BoboSegmentReader>();
            foreach (BoboMultiReader reader in readerList)
            {
                foreach (BoboSegmentReader subReader in reader.SubReaders)
                {
                    subReaderList.Add(subReader);
                }
            }
            return subReaderList;
        }

        public static IBrowsable[] CreateBrowsables(List<BoboSegmentReader> readerList)
        {
            BoboSubBrowser[] browsables = new BoboSubBrowser[readerList.Count];
            for (int i = 0; i < readerList.Count; ++i)
            {
                browsables[i] = new BoboSubBrowser(readerList[i]);
            }
            return browsables;
        }

        /// <summary>
        /// Gets a set of facet names.
        /// </summary>
        /// <returns>Set of facet names.</returns>
        public override IEnumerable<string> FacetNames
        {
            get 
            {
                if (_subBrowsers.Length == 0)
                {
                    return null;
                }
                return _subBrowsers[0].FacetNames; 
            }
        }

        /// <summary>
        /// Gets a facet handler by facet name.
        /// </summary>
        /// <param name="name">The facet name.</param>
        /// <returns>The facet handler instance.</returns>
        public override IFacetHandler GetFacetHandler(string name)
        {
            if (_subBrowsers.Length == 0)
            {
                return null;
            }
            return _subBrowsers[0].GetFacetHandler(name);
        }
    }
}
