//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Written in Java.
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

namespace BoboBrowse.Net.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using BoboBrowse.Net.Util;

    public class MultiTopDocsSortedHitCollector : TopDocsSortedHitCollector
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(MultiTopDocsSortedHitCollector));
        private int totalCount;
        private readonly MultiBoboBrowser multiBrowser;
        private readonly TopDocsSortedHitCollector[] subCollectors;
        private readonly int[] starts;
        private readonly int offset;
        private readonly int count;
        private readonly SortField[] sort;

        public MultiTopDocsSortedHitCollector(MultiBoboBrowser multiBrowser, SortField[] sort, int offset, int count,
                                              bool fetchStoredFields)
        {
            this.sort = sort;
            this.offset = offset;
            this.count = count;
            this.multiBrowser = multiBrowser;
            IBrowsable[] subBrowsers = this.multiBrowser.getSubBrowsers();
            subCollectors = new TopDocsSortedHitCollector[subBrowsers.Length];
            for (int i = 0; i < subBrowsers.Length; ++i)
            {
                subCollectors[i] = subBrowsers[i].GetSortedHitCollector(sort, 0, this.offset + this.count, fetchStoredFields);
            }
            starts = this.multiBrowser.getStarts();
            totalCount = 0;
        }

        public override void SetScorer(Scorer scorer)
        {
            //throw new System.NotImplementedException();
        }

        public override void Collect(int doc)
        {
            int mapped = multiBrowser.SubDoc(doc);
            int index = multiBrowser.SubSearcher(doc);
            subCollectors[index].Collect(mapped);
            totalCount++;
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            //throw new System.NotImplementedException();
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                return true;
            }
        }

        public override BrowseHit[] GetTopDocs()
        {
            List<IEnumerable<BrowseHit>> iteratorList = new List<IEnumerable<BrowseHit>>(subCollectors.Length);

            for (int i = 0; i < subCollectors.Length; ++i)
            {
                int @base = starts[i];
                try
                {
                    BrowseHit[] subHits = subCollectors[i].GetTopDocs();
                    foreach (BrowseHit hit in subHits)
                    {
                        hit.DocId = hit.DocId + @base;
                    }
                    iteratorList.Add(subHits);
                }
                catch (System.IO.IOException ioe)
                {
                    logger.Error(ioe.Message, ioe);
                }
            }

            SortField[] sf = sort;
            if (sf == null || sf.Length == 0)
            {
                sf = new SortField[] { SortField.FIELD_SCORE };
            }
            IComparer<BrowseHit> comparator = new SortedFieldBrowseHitComparator(sf);

            List<BrowseHit> mergedList = ListMerger.MergeLists(offset, count, iteratorList.ToArray(), comparator);
            return mergedList.ToArray();
        }

        public override int GetTotalHits()
        {
            return totalCount;
        }
    }
}
