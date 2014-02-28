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
    using Common.Logging;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using BoboBrowse.Net.Facets;

    internal class SortedHitQueue : PriorityQueue<FieldDocEntry>
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SortedHitQueue));
        private readonly BoboBrowser boboBrowser;

        /// <summary> Stores a comparator corresponding to each field being sorted by  </summary>
        protected internal FieldComparator[] comparators;
        internal Dictionary<string, FieldComparator> comparatorMap;
        protected internal bool[] isReverse;

        public SortedHitQueue(BoboBrowser boboBrowser, SortField[] sortFields, int size)
        {
            comparatorMap = new Dictionary<string, FieldComparator>();
            this.boboBrowser = boboBrowser;
            int n = sortFields.Length;
            List<FieldComparator> comparatorList = new List<FieldComparator>(n);
            List<bool> reverseList = new List<bool>(n);

            for (int i = 0; i < n; ++i)
            {
                FieldComparator comparator = GetScoreDocComparator(size, sortFields[i]);

                if (comparator != null)
                {
                    comparatorList.Add(comparator);
                    reverseList.Add(sortFields[i].Reverse);
                }
            }
            comparators = comparatorList.ToArray();
            isReverse = new bool[reverseList.Count];
            int c = 0;
            foreach (bool revVal in reverseList)
            {
                isReverse[c++] = revVal;
            }
            Initialize(size);
        }

        ///    
        ///	   <summary> * Returns whether <code>a</code> is less relevant than <code>b</code>. </summary>
        ///	   * <param name="a"> ScoreDoc </param>
        ///	   * <param name="b"> ScoreDoc </param>
        ///	   * <returns> <code>true</code> if document <code>a</code> should be sorted after document <code>b</code>. </returns>
        ///	   
        public override bool LessThan(FieldDocEntry a, FieldDocEntry b)
        {            

            // run comparators
            int c = 0;
            int i = 0;
            foreach (FieldComparator comparator in comparators)
            {
                c = comparator.Compare(a.Slot, b.Slot);
                if (c != 0)
                {
                    return isReverse[i] ? c < 0 : c > 0;
                }
                i++;
            }
            return a.Doc > b.Doc;
        }

        public virtual FieldDocEntry[] GetTopDocs(int offset, int numHits)
        {
            FieldDocEntry[] retVal = new FieldDocEntry[0];
            do
            {
                if (numHits == 0)
                    break;
                int size = this.Size();
                if (size == 0)
                    break;

                if (offset < 0 || offset >= size)
                {
                    throw new System.ArgumentException("Invalid offset: " + offset);
                }

                FieldDocEntry[] fieldDocs = new FieldDocEntry[size];
                for (int i = size - 1; i >= 0; i--)
                {
                    fieldDocs[i] = Pop();
                }          

                int count = Math.Min(numHits, (size - offset));
                retVal = new FieldDocEntry[count];
                int n = offset + count;
                // if distance is there for 1 hit, it's there for all
                for (int i = offset; i < n; ++i)
                {
                    FieldDocEntry hit = fieldDocs[i];
                    retVal[i - offset] = hit;
                }
            } while (false);
            return retVal;
        }

        internal virtual FieldComparator GetScoreDocComparator(int numDocs, SortField field)
        {
            int type = field.Type;          
            if (type == SortField.DOC || type==SortField.SCORE)
            {
                return field.GetComparator(numDocs, 0);
            }
            string f = field.Field;
            FacetHandler facetHandler = boboBrowser.GetFacetHandler(f);
            FieldComparator comparator = null;
            if (facetHandler != null)
            {
                comparator = facetHandler.GetScoreDocComparator();
            }
            if (comparator == null) // resort to lucene
            {
                try
                {
                    comparator = boboBrowser.GetIndexReader().GetDefaultScoreDocComparator(numDocs,field);
                }
                catch (System.IO.IOException ioe)
                {
                    logger.Error(ioe.Message, ioe);
                }
            }

            if (comparator != null)
            {
                comparatorMap.Add(f, comparator);
            }
            return comparator;
        }
    }
}
