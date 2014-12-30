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

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Util;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    ///<summary>
    /// This class implements the browsing functionality.
    /// author ymatsuda
    ///</summary>
    public class BoboBrowser : MultiBoboBrowser
    {
        private static ILog logger = LogManager.GetLogger<BoboBrowser>();

        ///<summary>Constructor.</summary>
        ///<param name="reader">A bobo reader instance</param>
        public BoboBrowser(BoboIndexReader reader)
            : base(CreateBrowsables(reader))
        {}

        public static void GatherSubReaders(IList<BoboIndexReader> readerList, BoboIndexReader reader)
        {
            BoboIndexReader[] subReaders = reader._subReaders;
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
        /// Gets a set of facet names
        /// </summary>
        /// <returns>set of facet names</returns>
        public override IEnumerable<string> FacetNames
        {
            get { return _subBrowsers[0].FacetNames; }
        }

        public override IFacetHandler GetFacetHandler(string name)
        {
            return _subBrowsers[0].GetFacetHandler(name);
        }
    }
}
