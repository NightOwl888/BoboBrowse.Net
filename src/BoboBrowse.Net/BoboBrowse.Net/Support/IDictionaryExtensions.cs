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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Support
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class IDictionaryExtensions
    {
        public static void Put<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
            else
                dictionary[key] = value;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (key == null) 
                return default(TValue);
            TValue value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            else
                return default(TValue);
        }

        public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> values)
        {
            foreach (var pair in values)
            {
                Put(dictionary, pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Converts a dictionary of string, Type to a display string by calling ToString() on the inner type.
        /// Note that this won't work if TValue is an IEnumerable or IDictionary.
        /// </summary>
        /// <typeparam name="TValue">The type of object in dictionary that overrides ToString().</typeparam>
        /// <param name="dictionary">A dictionary with a string key.</param>
        /// <returns>A string suitable for display or debugging.</returns>
        public static string ToDisplayString<TValue>(this IDictionary<string, TValue> dictionary)
        {
            if (dictionary == null)
            {
                return string.Empty;
            }

            return "{" + string.Join(", ", dictionary.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value.ToString())).ToArray()) + "}";
        }
    }
}
