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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;
    using System.Linq;

    public static class IEnumerableExtensions
    {
        public static T Get<T>(this IEnumerable<T> enumerable, int index) where T : class
        {
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);
            else
                return default(T);
        }

        public static T Get<T>(this IEnumerable<T> enumerable, int index, T defaultValue)
        {
            if (index < enumerable.Count())
                return enumerable.ElementAt(index);
            else
                return defaultValue;
        }

        /// <summary>
        /// Converts an IEnumerable to a display string by calling the ToString() method of the inner type.
        /// Note that this won't work if the inner type is IEnumerable or IDictionary.
        /// </summary>
        /// <typeparam name="T">The enumerable object type that overrides ToString().</typeparam>
        /// <param name="enumerable">An IEnumerable instance.</param>
        /// <returns>A string suitable for display or debugging.</returns>
        public static string ToDisplayString<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return string.Empty;
            }

            return "{" + string.Join(", ", enumerable.Select(x => x.ToString()).ToArray()) + "}";
        }
    }
}
