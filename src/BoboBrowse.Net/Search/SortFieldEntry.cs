//*
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
    using System.Globalization;
    using Lucene.Net.Search;

    public class SortFieldEntry
    {
        internal readonly string Field; // which Fieldable
        internal readonly int Type; // which SortField type
        internal readonly FieldComparatorSource Custom; // which custom comparator
        internal readonly CultureInfo Locale; // the locale we're sorting (if string)

        /// <summary> Creates one of these objects.  </summary>
        public SortFieldEntry(string field, int type, CultureInfo locale)
        {
            Field = string.Intern(field);
            Type = type;
            Custom = null;
            Locale = locale;
        }

        /// <summary> Creates one of these objects for a custom comparator.  </summary>
        public SortFieldEntry(string field, FieldComparatorSource custom)
        {
            Field = string.Intern(field);
            Type = SortField.CUSTOM;
            Custom = custom;
            Locale = null;
        }

        ///<summary>Two of these are equal iff they reference the same field and type.  </summary>
        public override bool Equals(object o)
        {
            if (o is SortFieldEntry)
            {
                SortFieldEntry other = (SortFieldEntry)o;
                if (other.Field == Field && other.Type == Type)
                {
                    if (other.Locale == null ? Locale == null : other.Locale.Equals(Locale))
                    {
                        if (other.Custom == null)
                        {
                            if (Custom == null)
                            {
                                return true;
                            }
                        }
                        else if (other.Custom.Equals(Custom))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        ///<summary>Composes a hashcode based on the field and type.  </summary>
        public override int GetHashCode()
        {
            return Field.GetHashCode() ^ Type ^ (Custom == null ? 0 : Custom.GetHashCode()) ^ (Locale == null ? 0 : Locale.GetHashCode());
        }
    }
}
