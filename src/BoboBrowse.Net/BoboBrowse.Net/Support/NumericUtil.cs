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
    using Lucene.Net.Util;
    using System;
    using System.Reflection;

    public class NumericUtil
    {
        public static bool IsNumeric(object value)
        {
            if (Equals(value, null))
            {
                return false;
            }

            Type objType = value.GetType();
            objType = Nullable.GetUnderlyingType(objType) ?? objType;

            if (objType.GetTypeInfo().IsPrimitive)
            {
                return objType != typeof(bool) &&
                    objType != typeof(char) &&
                    objType != typeof(IntPtr) &&
                    objType != typeof(UIntPtr);
            }

            return objType == typeof(decimal);
        }

        /// <summary>
        /// NOTE: This was IsPrefixCodedInt() in Java
        /// </summary>
        public static bool IsPrefixCodedInt32(string prefixCoded)
        {
            try
            {
                int shift = prefixCoded[0] - NumericUtils.SHIFT_START_INT32;
                if (shift > 31 || shift < 0)
                    return false;
                int sortableBits = 0;
                for (int i = 1, len = prefixCoded.Length; i < len; i++)
                {
                    sortableBits <<= 7;
                    char ch = prefixCoded[i];
                    if (ch > 0x7f)
                    {
                        return false;
                    }
                    sortableBits |= (int)ch;
                }
                int result = (sortableBits << shift) ^ unchecked((int)0x80000000);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// NOTE: This was IsPrefixCodedLong() in Java
        /// </summary>
        public static bool IsPrefixCodedInt64(string prefixCoded)
        {
            try
            {
                int shift = prefixCoded[0] - NumericUtils.SHIFT_START_INT64;
                if (shift > 63 || shift < 0)
                    return false;
                ulong sortableBits = 0UL;
                for (int i = 1, len = prefixCoded.Length; i < len; i++)
                {
                    sortableBits <<= 7;
                    char ch = prefixCoded[i];
                    if (ch > 0x7f)
                    {
                        return false;
                    }
                    sortableBits |= (ulong)ch;
                }
                long result = BitConverter.ToInt64(BitConverter.GetBytes((sortableBits << shift) ^ 0x8000000000000000L), 0);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// NOTE: This was IsPrefixCodedFloat() in Java
        /// </summary>
        public static bool IsPrefixCodedSingle(string prefixCoded)
        {
            if (IsPrefixCodedInt32(prefixCoded))
            {
                try
                {
                    int val = NumericUtils.PrefixCodedToInt32(new BytesRef(prefixCoded));
                    if (val < 0)
                        val ^= 0x7fffffff;
                    float result = BitConverter.ToSingle(BitConverter.GetBytes(val), 0);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public static bool IsPrefixCodedDouble(string prefixCoded)
        {
            if (IsPrefixCodedInt64(prefixCoded))
            {
                try
                {
                    long val = NumericUtils.PrefixCodedToInt64(new BytesRef(prefixCoded));
                    if (val < 0)
                        val ^= 0x7fffffffffffffffL;
                    double result = BitConverter.Int64BitsToDouble(val);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
