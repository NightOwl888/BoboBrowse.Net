//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
//*
//* Copyright (C) 2015  Shad Storhaug
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

namespace BoboBrowse.Net.Support
{
    using BoboBrowse.Net.Facets.Filter;
    using Lucene.Net.Documents;
    using System;
    
    /// <summary>
    /// Provides the means to customize the format of the range strings of a facet for display on the user interface.<br/>
    /// <br/>
    /// This class is intended for use with the <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> type. A 
    /// <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> requires a very rigid string format to be supplied to it.
    /// The range string has 3 purposes:
    /// <list type="number">
    ///     <item>
    ///         <description>To define the lower and upper bound of the range</description>
    ///     </item>
    ///     <item>
    ///         <description>To provide lexical sort order so the range facets can be sorted ascending or descending.</description>
    ///     </item>
    ///     <item>
    ///         <description>To act as a key for facet value selection.</description>
    ///     </item>
    /// </list>
    /// Because of these constraints, it is best to leave the range strings that are passed to the 
    /// <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> in this rigid format. This class 
    /// is intended as an aid to be able to format the ranges in a more user-friendly way for display on the user interface.
    /// Note that you still will need to track the values of the facets so selections by the user can be made 
    /// using the original facet value string.
    /// </summary>
    /// <typeparam name="T">The underlying data type of the facet handler.</typeparam>
    [Serializable]
    public class RangeStringFormatter<T> : RangeStringFormatter
    {
        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound.  
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        public RangeStringFormatter(string format)
            : base(typeof(T), format, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound. 
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public RangeStringFormatter(string format, IFormatProvider provider)
            : base(typeof(T), format, null, null, provider)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound.  
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="openLowerBoundFormat">The new format of the open lower bound string, if different from the format. 
        /// This can be used to provide a customized format string, such as "Less than $10.00" when the lower bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="openUpperBoundFormat">The new format of the open upper bound string, if different from the format.
        /// This can be used to provide a customized format string, such as "$500.00 and Up" when the upper bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        public RangeStringFormatter(string format, string openLowerBoundFormat, string openUpperBoundFormat)
            : base(typeof(T), format, openLowerBoundFormat, openUpperBoundFormat, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound. 
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="openLowerBoundFormat">The new format of the open lower bound string, if different from the format. 
        /// This can be used to provide a customized format string, such as "Less than $10.00" when the lower bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="openUpperBoundFormat">The new format of the open upper bound string, if different from the format.
        /// This can be used to provide a customized format string, such as "$500.00 and Up" when the upper bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public RangeStringFormatter(string format, string openLowerBoundFormat, string openUpperBoundFormat, IFormatProvider provider)
            : base(typeof(T), format, openLowerBoundFormat, openUpperBoundFormat, provider)
        {
        }
    }

    /// <summary>
    /// Provides the means to customize the format of the range strings of a facet for display on the user interface.<br/>
    /// <br/>
    /// This class is intended for use with the <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> type. A 
    /// <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> requires a very rigid string format to be supplied to it.
    /// The range string has 3 purposes:
    /// <list type="number">
    ///     <item>
    ///         <description>To define the lower and upper bound of the range</description>
    ///     </item>
    ///     <item>
    ///         <description>To provide lexical sort order so the range facets can be sorted ascending or descending.</description>
    ///     </item>
    ///     <item>
    ///         <description>To act as a key for facet value selection.</description>
    ///     </item>
    /// </list>
    /// Because of these constraints, it is best to leave the range strings that are passed to the 
    /// <see cref="T:BoboBrowse.Net.Facets.Impl.RangeFacetHandler"/> in this rigid format. This class 
    /// is intended as an aid to be able to format the ranges in a more user-friendly way for display on the user interface.
    /// Note that you still will need to track the values of the facets so selections by the user can be made 
    /// using the original facet value string.
    /// </summary>
    [Serializable]
    public class RangeStringFormatter
    {
        private readonly Type type;
        private readonly string format;
        private readonly IFormatProvider provider;
        private readonly string openLowerBoundFormat;
        private readonly string openUpperBoundFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="type">The underlying data type of the facet handler.</param>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound.  
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        public RangeStringFormatter(Type type, string format)
            : this(type, format, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="type">The underlying data type of the facet handler.</param>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound. 
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public RangeStringFormatter(Type type, string format, IFormatProvider provider)
            : this(type, format, null, null, provider)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="type">The underlying data type of the facet handler.</param>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound.  
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="openLowerBoundFormat">The new format of the open lower bound string, if different from the format. 
        /// This can be used to provide a customized format string, such as "Less than $10.00" when the lower bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="openUpperBoundFormat">The new format of the open upper bound string, if different from the format.
        /// This can be used to provide a customized format string, such as "$500.00 and Up" when the upper bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        public RangeStringFormatter(Type type, string format, string openLowerBoundFormat, string openUpperBoundFormat)
            : this(type, format, openLowerBoundFormat, openUpperBoundFormat, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="type">The underlying data type of the facet handler.</param>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound. 
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        /// <param name="openLowerBoundFormat">The new format of the open lower bound string, if different from the format. 
        /// This can be used to provide a customized format string, such as "Less than $10.00" when the lower bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="openUpperBoundFormat">The new format of the open upper bound string, if different from the format.
        /// This can be used to provide a customized format string, such as "$500.00 and Up" when the upper bound of the range is "*".
        /// Set this value to null to default to the value of the format parameter.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        public RangeStringFormatter(Type type, string format, string openLowerBoundFormat, string openUpperBoundFormat, IFormatProvider provider)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException("format");

            this.type = type;
            this.format = format;
            this.provider = provider;
            this.openLowerBoundFormat = string.IsNullOrEmpty(openLowerBoundFormat) ? format : openLowerBoundFormat;
            this.openUpperBoundFormat = string.IsNullOrEmpty(openUpperBoundFormat) ? format : openUpperBoundFormat;
        }

        /// <summary>
        /// Provides a custom format for a range string for display on the user interface.
        /// </summary>
        /// <param name="rangeString">A range string that represents a browse facet.</param>
        /// <returns>The custom formatted range string.</returns>
        public string Format(string rangeString)
        {
            object lower, upper;
            bool lowerOpen, upperOpen;
            this.ParseValues(rangeString, out lower, out lowerOpen, out upper, out upperOpen);

            string currentFormat = lowerOpen ? this.openLowerBoundFormat : (upperOpen ? this.openUpperBoundFormat : this.format);

            return string.Format(this.provider, currentFormat, lower, upper);
        }

        /// <summary>
        /// Parses the individual values out of a range string.
        /// </summary>
        /// <param name="rangeString">The range string.</param>
        /// <param name="lower">The lower bound in its native type.</param>
        /// <param name="lowerOpen">True if the lower bound is open-ended; otherwise false.</param>
        /// <param name="upper">The upper bound in its native type.</param>
        /// <param name="upperOpen">True if the upper bound is open-ended; otherwise false.</param>
        protected virtual void ParseValues(string rangeString, out object lower, out bool lowerOpen, out object upper, out bool upperOpen)
        {
            var values = FacetRangeFilter.GetRangeStrings(rangeString);
            var lowerBound = values[0];
            var upperBound = values[1];

            if (lowerBound.Equals("*"))
            {
                lowerOpen = true;
                lower = "*";
            }
            else
            {
                lowerOpen = false;
                lower = Parse(lowerBound);
            }

            if (upperBound.Equals("*"))
            {
                upperOpen = true;
                upper = "*";
            }
            else
            {
                upperOpen = false;
                upper = Parse(upperBound);
            }
        }

        /// <summary>
        /// Parses a range value into its native type.
        /// </summary>
        /// <param name="value">A range value.</param>
        /// <returns>The range value converted to its native type.</returns>
        protected virtual object Parse(string value)
        {
            // Optimization - don't bother calling 
            if (this.type == typeof(string))
            {
                return value;
            }

            try
            {
                return Convert.ChangeType(value, this.type);
            }
            catch
            {
                if (this.type == typeof(DateTime))
                {
                    // Attempt to use the Lucene.Net date conversion if it fails to parse
                    return DateTools.StringToDate(value);
                }
                throw;
            }
        }
    }
}
