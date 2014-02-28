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
    using System.Globalization;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Search;

    public class LuceneSortDocComparatorFactory
    {
        private static ILog logger = LogManager.GetLogger(typeof(LuceneSortDocComparatorFactory));

        public static FieldComparator BuildScoreDocComparator(IndexReader reader,int numDocs, SortFieldEntry entry)
        {
            string fieldname = entry.Field;
            int type = entry.Type;

            var sortField = entry.Locale != null ? new SortField(fieldname, entry.Locale) : new SortField(fieldname, type);
            return sortField.GetComparator(numDocs, 0);
        }
    }
}
