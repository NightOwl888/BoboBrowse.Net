//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2005-2015  John Wang
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

// Version compatibility level: 4.0.2
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
        private readonly Type m_type;
        private readonly Type m_listType;
        private readonly string m_formatString;
        private readonly IFormatProvider m_formatProvider;
        protected IDictionary<Type, Type> m_supportedTypes = new Dictionary<Type, Type>()
        {
            { typeof(int), typeof(TermInt32List) },
            { typeof(float), typeof(TermSingleList) },
            { typeof(char), typeof(TermCharList) },
            { typeof(double), typeof(TermDoubleList) },
            { typeof(short), typeof(TermInt16List) },
            { typeof(long), typeof(TermInt64List) },
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
            if (!m_supportedTypes.ContainsKey(type))
                throw new ArgumentException(string.Format("Type '{0}' is not supported. The only supported types are:{2}{1}",
                    type.FullName, string.Join(Environment.NewLine, m_supportedTypes.Keys.Select(key => key.FullName).ToArray()), Environment.NewLine));

            this.m_type = type;
            this.m_listType = m_supportedTypes[type];
            this.m_formatString = formatString;
            this.m_formatProvider = formatProvider;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory"/>.
        /// </summary>
        /// <param name="type">The native type of the values in the list. 
        /// Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, 
        /// <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</param>
        /// <param name="formatString">The format string that will be used to format each value in the list for output display.</param>
        public PredefinedTermListFactory(Type type, string formatString)
            : this(type, formatString, null)
        { }

        /// <summary>
        /// Initializes a new instance of <see cref="T:PredefinedTermListFactory"/>.
        /// </summary>
        /// <param name="type">The native type of the values in the list. 
        /// Supported types are <see cref="T:System.Int32"/>, <see cref="T:System.Single"/>, <see cref="T:System.Char"/>, 
        /// <see cref="T:System.Double"/>, <see cref="T:System.Int16"/>, <see cref="T:System.Int64"/>, <see cref="T:System.DateTime"/>.</param>
        public PredefinedTermListFactory(Type type)
            : this(type, null, null)
        { }

        public override ITermValueList CreateTermList(int capacity)
        {
            // we treat char type separate as it does not have a format string
            if (typeof(TermCharList).Equals(this.m_listType))
            {
                return new TermCharList();
            }
            else
            {
                return (ITermValueList)Activator.CreateInstance(this.m_listType, capacity, this.m_formatString, this.m_formatProvider);
            }
        }

        public override ITermValueList CreateTermList()
        {
            // In .NET, the initial capacity is 0.
            return CreateTermList(0);
        }

        public override Type Type
        {
            get { return this.m_type; }
        }
    }
}