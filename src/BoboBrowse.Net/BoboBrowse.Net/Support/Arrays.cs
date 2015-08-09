//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
//*
//* Copyright (C) 2011-2015  Alexey Shcherbachev
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
    using System.Linq;

    internal static class Arrays
    {
        public static void Fill<T>(T[] block, T value)
        {
            for (int i = 0; i < block.Length; i++)
            {
                block[i] = value;
            }
        }

        public static string ToString<T>(T[] block)
        {
            if (block == null) 
                return string.Empty;
            return string.Join(", ", block.Select(x => x.ToString()).ToArray());
        }

        public static bool Equals<T>(T[] value1, T[] value2)
        {
            if (value1 == null)
                if (value2 == null)
                    return true;
                else
                    return false;

            if (value1.Length != value2.Length)
                return false;

            for (int i = 0; i < value1.Length; i++)
            {
                if (!value1[i].Equals(value2[i]))
                    return false;
            }

            return true;
        }

        public static int HashCode(long[] array)
        {
            if (array == null)
            {
                return 0;
            }
            int hashCode = 1;
            foreach (long elementValue in array)
            {
                /*
                 * the hash code value for long value is (int) (value ^ (value >>>
                 * 32))
                 */
                hashCode = 31 * hashCode
                        + (int)(elementValue ^ (long)(((ulong)elementValue) >> 32));
            }
            return hashCode;
        }
    }
}
