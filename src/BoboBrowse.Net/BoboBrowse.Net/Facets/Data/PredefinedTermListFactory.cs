// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    /// <typeparam name="TSupported">The type of term list to build. Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</typeparam>
    public class PredefinedTermListFactory<TSupported> : PredefinedTermListFactory
    {
        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory{TSupported}"/>.
        /// </summary>
        /// <param name="formatString">The format string that will be used to format each value in the list for output display.</param>
        /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
        public PredefinedTermListFactory(string formatString, IFormatProvider formatProvider)
            : base(typeof(TSupported), formatString, formatProvider)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory{TSupported}"/>.
        /// </summary>
        /// <param name="formatString">The format string that will be used to format each value in the list for output display.</param>
        public PredefinedTermListFactory(string formatString)
            : this(formatString, null)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory{TSupported}"/>.
        /// </summary>
        public PredefinedTermListFactory()
            : this(null, null)
        { }
    }

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
    public class PredefinedTermListFactory: TermListFactory
    {
        private readonly Type listType;
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

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory"/>.
        /// </summary>
        /// <param name="type">The native type of the values in the list. 
        /// Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, 
        /// <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</param>
        /// <param name="formatString">The format string that will be used to format each value in the list for output display.</param>
        /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
        public PredefinedTermListFactory(Type type, string formatString, IFormatProvider formatProvider)
        {
            if (!supportedTypes.ContainsKey(type))
                throw new ArgumentException(string.Format("Type '{0}' is not supported. The only supported types are:{2}{1}",
                    type.FullName, string.Join(Environment.NewLine, supportedTypes.Keys.Select(key => key.FullName).ToArray()), Environment.NewLine));

            this.listType = supportedTypes[type];
            this.formatString = formatString;
            this.formatProvider = formatProvider;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory"/>.
        /// </summary>
        /// <param name="listType">The native type of the values in the list. 
        /// Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, 
        /// <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</param>
        /// <param name="formatString">The format string that will be used to format each value in the list for output display.</param>
        public PredefinedTermListFactory(Type listType, string formatString)
            : this(listType, formatString, null)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory"/>.
        /// </summary>
        /// <param name="listType">The native type of the values in the list. 
        /// Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, 
        /// <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</param>
        public PredefinedTermListFactory(Type listType)
            : this(listType, null, null)
        { }

        public override ITermValueList CreateTermList(int capacity)
        {
            // we treat char type separate as it does not have a format string
            if (typeof(TermCharList).Equals(listType))
            {
                return new TermCharList();
            }
            else
            {
                return (ITermValueList)Activator.CreateInstance(listType, capacity, this.formatString, this.formatProvider);
            }
        }

        public override ITermValueList CreateTermList()
        {
            // In .NET, the initial capacity is 0.
            return CreateTermList(0);
        }
    }
}