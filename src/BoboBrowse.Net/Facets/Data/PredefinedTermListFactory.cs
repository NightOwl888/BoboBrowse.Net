// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Similar to the original Java version of the PredefinedTermListFactory, but has been optimized to use
    /// .NET generics and .NET string formatting.
    /// 
    /// Types supported:
    /// <list type="bullet">
    ///     <item>int</item>
    ///     <item>float</item>
    ///     <item>char</item>
    ///     <item>double</item>
    ///     <item>short</item>
    ///     <item>long</item>
    ///     <item>DateTime</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TSupported">The type of term list to build. Supported types are int, float, char, double, long, and DateTime.</typeparam>
    public class PredefinedTermListFactory<TSupported> : TermListFactory
    {
        private readonly string formatString;
        private readonly IFormatProvider formatProvider;
        protected IDictionary<Type, Type> supportedTypes = new Dictionary<Type, Type>()
        {
            { typeof(int), typeof(TermIntList) },
            { typeof(float), typeof(TermFloatList) },
            { typeof(char), typeof(TermCharList) },
            { typeof(double), typeof(TermDoubleList) },
            { typeof(short), typeof(TermShortList) },
            { typeof(long), typeof(TermLongList) },
            { typeof(DateTime), typeof(TermDateList) }
        };

        public PredefinedTermListFactory(string formatString, IFormatProvider formatProvider)
        {
            if (!this.supportedTypes.ContainsKey(typeof(TSupported)))
                throw new ArgumentException(string.Format("Type '{0}' is not supported. The only supported types are:{2}{1}",
                    typeof(TSupported).FullName, string.Join(Environment.NewLine, this.supportedTypes.Keys.Select(key => key.FullName).ToArray()), Environment.NewLine));

            this.formatString = formatString;
            this.formatProvider = formatProvider;
        }

        public PredefinedTermListFactory(string formatString)
            : this(formatString, null)
        { }

        public PredefinedTermListFactory()
            : this(null, null)
        { }

        public override ITermValueList CreateTermList()
        {
            var listType = this.supportedTypes[typeof(TSupported)];
            // we treat char type separate as it does not have a format string
            if (typeof(TermCharList).Equals(listType))
            {
                return new TermCharList();
            }
            else
            {
                return (ITermValueList)Activator.CreateInstance(listType, this.formatString, this.formatProvider);
            }
        }
    }
}