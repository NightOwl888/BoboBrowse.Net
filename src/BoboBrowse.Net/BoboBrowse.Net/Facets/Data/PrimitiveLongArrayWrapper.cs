//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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

﻿// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Support;

    /// <summary>
    /// NOTE: This was PrimitiveLongArrayWrapper in bobo-browse
    /// </summary>
    public class PrimitiveInt64ArrayWrapper
    {
        private long[] m_data;

        public PrimitiveInt64ArrayWrapper(long[] data)
        {
            this.m_data = data;
        }

        /// <summary>
        /// Added in .NET version as an accessor to the data field.
        /// </summary>
        /// <returns></returns>
        public virtual long[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        public override bool Equals(object other)
        {
            if (other is PrimitiveInt64ArrayWrapper)
            {
                return Arrays.Equals(m_data, ((PrimitiveInt64ArrayWrapper)other).m_data);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(m_data);
        }
    }
}
