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
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using Common.Logging;
    using System.Collections.Generic;
    using System.Linq;

    ///<summary>
    /// This class implements the browsing functionality.
    /// author ymatsuda
    ///</summary>
    public class BoboBrowser : MultiBoboBrowser
    {
        private static ILog logger = LogManager.GetLogger(typeof(BoboBrowser));

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BoboBrowser"/> class with the specified <see cref="T:BoboIndexReader"/> instance.
        /// </summary>
        /// <param name="reader">An open <see cref="T:BoboIndexReader"/> instance.</param>
        public BoboBrowser(BoboIndexReader reader)
            : base(CreateBrowsables(reader))
        {}

        public static void GatherSubReaders(IList<BoboIndexReader> readerList, BoboIndexReader reader)
        {
            BoboIndexReader[] subReaders = reader.SubReaders;
            if (subReaders == null)
            {
                readerList.Add(reader);
            }
            else
            {
                for (int i = 0; i < subReaders.Length; i++)
                {
                    GatherSubReaders(readerList, subReaders[i]);
                }  
            }
        }

        public static BoboSubBrowser[] CreateSegmentedBrowsables(IEnumerable<BoboIndexReader> readerList)
        {
            BoboSubBrowser[] browsables = new BoboSubBrowser[readerList.Count()];
            int i = 0;
            foreach (BoboIndexReader reader in readerList)
            {
                browsables[i] = new BoboSubBrowser(reader);
                i++;
            }
            return browsables;
        }

        public static IBrowsable[] CreateBrowsables(BoboIndexReader reader)
        {
            List<BoboIndexReader> readerList = new List<BoboIndexReader>();
            GatherSubReaders(readerList, reader);
            return CreateSegmentedBrowsables(readerList);
        }

        public static IEnumerable<BoboIndexReader> GatherSubReaders(IList<BoboIndexReader> readerList)
        {
            List<BoboIndexReader> subreaderList = new List<BoboIndexReader>();
            foreach (BoboIndexReader reader in readerList)
            {
                GatherSubReaders(subreaderList, reader);
            }
            return subreaderList;
        }

        public static IBrowsable[] CreateBrowsables(IList<BoboIndexReader> readerList)
        {
            var subreaders = GatherSubReaders(readerList);
            return CreateSegmentedBrowsables(subreaders);
        }

        /// <summary>
        /// Gets a set of facet names.
        /// </summary>
        /// <returns>Set of facet names.</returns>
        public override IEnumerable<string> FacetNames
        {
            get { return _subBrowsers[0].FacetNames; }
        }

        /// <summary>
        /// Gets a facet handler by facet name.
        /// </summary>
        /// <param name="name">The facet name.</param>
        /// <returns>The facet handler instance.</returns>
        public override IFacetHandler GetFacetHandler(string name)
        {
            return _subBrowsers[0].GetFacetHandler(name);
        }
    }
}
