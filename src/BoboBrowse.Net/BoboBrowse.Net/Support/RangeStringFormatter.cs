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
    public class RangeStringFormatter<T>
    {
        private readonly string format;
        private readonly IFormatProvider provider;
        private readonly string openLowerBoundFormat;
        private readonly string openUpperBoundFormat;

        /// <summary>
        /// Initializes a new instance of <see cref="T:RangeFacetFormatter{T}"/>.
        /// </summary>
        /// <param name="format">The new format of the range string. The format is equivalent to the format that is used 
        /// in the <see cref="M:System.String.Format"/> method; the parameter placeholders must be supplied with curly brackets. 
        /// There are 2 parameters, 0 for lower bound and 1 for upper bound.  
        /// Example: <![CDATA["{0:c} to {1:c}({2})"]]>.</param>
        public RangeStringFormatter(string format)
            : this(format, null, null, null)
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
            : this(format, null, null, provider)
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
            : this(format, openLowerBoundFormat, openUpperBoundFormat, null)
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
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException("format");

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
        protected virtual T Parse(string value)
        {
            // Optimization - don't bother calling 
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                if (typeof(T) == typeof(DateTime))
                {
                    // Attempt to use the Lucene.Net date conversion if it fails to parse
                    return (T)(object)DateTools.StringToDate(value);
                }
                throw;
            }
        }
    }
}
