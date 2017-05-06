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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net.Facets
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The dummy interface to indicate that a class type can be used for initializing RuntimeFacetHandlers.
    /// 
    /// author xiaoyang
    /// </summary>
    [Serializable]
    public abstract class FacetHandlerInitializerParam
    {
        public static readonly FacetHandlerInitializerParam EMPTY_PARAM = new EmptyFacetHandlerInitializerParam();

        private sealed class EmptyFacetHandlerInitializerParam : FacetHandlerInitializerParam
        {
            public override IEnumerable<string> GetStringParam(string name)
            {
                return new string[0];
            }

            public override int[] GetInt32Param(string name)
            {
                return new int[0];
            }

            public override bool[] GetBooleanParam(string name)
            {
                return new bool[0];
            }

            public override long[] GetInt64Param(string name)
            {
                return new long[0];
            }

            public override byte[] GetByteArrayParam(string name)
            {
                return new byte[0];
            }

            public override double[] GetDoubleParam(string name)
            {
                return new double[0];
            }

            public override ICollection<string> BooleanParamNames
            {
                get { return new List<string>(); }
            }

            public override ICollection<string> StringParamNames
            {
                get { return new List<string>(); }
            }

            public override ICollection<string> Int32ParamNames
            {
                get { return new List<string>(); }
            }

            public override ICollection<string> ByteArrayParamNames
            {
                get { return new List<string>(); }
            }

            public override ICollection<string> Int64ParamNames
            {
                get { return new List<string>(); }
            }

            public override ICollection<string> DoubleParamNames
            {
                get { return new List<string>(); }
            }
        }

        //private static long serialVersionUID = 1L; // NOT USED

        /// <summary>
        /// The transaction ID
        /// </summary>
        private long m_tid = -1;

        /// <summary>
        /// Get or sets the transaction ID.
        /// </summary>
        /// <returns>the transaction ID.</returns>
        public long Tid 
        { 
            get { return m_tid; }
            set { this.m_tid = value; }
        }

        public abstract IEnumerable<string> GetStringParam(string name);
        public abstract int[] GetInt32Param(string name);
        public abstract bool[] GetBooleanParam(string name);
        public abstract long[] GetInt64Param(string name);
        public abstract byte[] GetByteArrayParam(string name);
        public abstract double[] GetDoubleParam(string name);
        public abstract ICollection<string> BooleanParamNames { get; }
        public abstract ICollection<string> StringParamNames { get; }
        public abstract ICollection<string> Int32ParamNames { get; }
        public abstract ICollection<string> ByteArrayParamNames { get; }
        public abstract ICollection<string> Int64ParamNames { get; }
        public abstract ICollection<string> DoubleParamNames { get; }
    }
}
