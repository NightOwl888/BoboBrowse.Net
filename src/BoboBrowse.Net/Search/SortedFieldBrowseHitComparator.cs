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

    public class SortedFieldBrowseHitComparator : IComparer<BrowseHit>
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SortedFieldBrowseHitComparator));

        private readonly SortField[] sortFields;

        public SortedFieldBrowseHitComparator(SortField[] sortFields)
        {
            this.sortFields = sortFields;
        }

        private int Compare(BrowseHit h1, BrowseHit h2, SortField sort)
        {            
            int c = 0;

            switch (sort.Type)
            {
                case SortField.SCORE:
                    {
                        float r1 = h1.Score;
                        float r2 = h2.Score;
                        if (r1 > r2)
                            c = -1;
                        if (r1 < r2)
                            c = 1;
                        break;
                    }
                case SortField.DOC:
                    {
                        int i1 = h1.DocId;
                        int i2 = h2.DocId;
                        c = i2 - i1;
                        break;
                    }
                case SortField.INT:
                    {
                        int i1 = ((int)h1.GetComparable(sort.Field));
                        int i2 = ((int)h2.GetComparable(sort.Field));
                        c = i1 - i2;
                        break;
                    }
                case SortField.LONG:
                    {
                        long l1 = ((long)h1.GetComparable(sort.Field));
                        long l2 = ((long)h2.GetComparable(sort.Field));
                        if (l1 < l2)
                            c = -1;
                        if (l1 > l2)
                            c = 1;
                        break;
                    }
                case SortField.STRING:
                    {
                        string s1 = (string)h1.GetField(sort.Field);
                        string s2 = (string)h2.GetField(sort.Field);
                        if (s1 == null)
                        {
                            if (s2 == null)
                                c = 0;
                            else
                                c = 1;
                        }
                        else
                        {
                            c = s1.CompareTo(s2);
                        }
                        break;
                    }
                case SortField.FLOAT:
                    {
                        float f1 = ((float)h1.GetComparable(sort.Field));
                        float f2 = ((float)h2.GetComparable(sort.Field));
                        if (f1 < f2)
                            c = -1;
                        if (f1 > f2)
                            c = 1;
                        break;
                    }
                case SortField.DOUBLE:
                    {
                        double d1 = ((double)h1.GetComparable(sort.Field));
                        double d2 = ((double)h2.GetComparable(sort.Field));
                        if (d1 < d2)
                            c = -1;
                        if (d1 > d2)
                            c = 1;
                        break;
                    }
                case SortField.BYTE:
                    {
                        int i1 = ((sbyte)h1.GetComparable(sort.Field));
                        int i2 = ((sbyte)h2.GetComparable(sort.Field));
                        c = i1 - i2;
                        break;
                    }
                case SortField.SHORT:
                    {
                        int i1 = ((short)h1.GetComparable(sort.Field));
                        int i2 = ((short)h2.GetComparable(sort.Field));
                        c = i1 - i2;
                        break;
                    }
                case SortField.CUSTOM:
                    {                        
                        IComparable obj1 = h1.GetComparable(sort.Field);
                        IComparable obj2 = h2.GetComparable(sort.Field);
                        if (obj1 == null)
                        {
                            if (obj2 == null)
                                c = 0;
                            else
                                c = 1;
                        }
                        else
                        {
                            c = obj1.CompareTo(obj2);
                        }
                        break;
                    }
                default:
                    {
                        throw new RuntimeException("invalid SortField type: " + sort.Type);
                    }
            }

            if (sort.Reverse)
            {
                c = -c;
            }

            return c;
        }

        public virtual int Compare(BrowseHit h1, BrowseHit h2)
        {
            foreach (SortField sort in sortFields)
            {
                int val = Compare(h1, h2, sort);
                if (val != 0)
                    return val;
            }
            return h2.DocId - h1.DocId;
        }
    }
}
