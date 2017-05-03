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
    using System.Collections.Generic;

    public static class ListExtensions
    {
        public static T Poll<T>(this IList<T> list) where T : class
        {
            T value = list.Get(0);
            if (list.Count > 0)
                list.RemoveAt(0);
            return value;
        }

        public static T Poll<T>(this IList<T> list, T defaultValue)
        {
            T value = list.Get(0, defaultValue);
            if (list.Count > 0)
                list.RemoveAt(0);
            return value;
        }

        /// <summary>
        /// Removes elements of this type-specific list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="from">the start index (inclusive).</param>
        /// <param name="to">the end index (exclusive).</param>
        public static void RemoveElements<T>(this IList<T> list, int from, int to)
        {
            for (int i = (to - 1); i >= from; i--)
            {
                list.RemoveAt(i);
            }
        }
    }
}
