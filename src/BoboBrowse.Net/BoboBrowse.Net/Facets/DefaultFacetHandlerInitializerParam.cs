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
    using BoboBrowse.Net.Support;
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The 'generic' type for FacetHandler initialization parameters for the purpose of easy serialization.
    /// When this type is used, it is completely up to the program logic of the utilizing RuntimeFacetHandler
    /// and its client code to find the data at the right place.
    /// 
    /// author ymatsuda
    /// </summary>
    public class DefaultFacetHandlerInitializerParam : FacetHandlerInitializerParam
    {
        //private static long serialVersionUID = 1L; // NOT USED
        private readonly IDictionary<string, bool[]> m_boolMap;
        private readonly IDictionary<string, int[]> m_intMap;
        private readonly IDictionary<string, long[]> m_longMap;
        private readonly IDictionary<string, IList<string>> m_stringMap;
        private readonly IDictionary<string, byte[]> m_byteMap;
        private readonly IDictionary<string, double[]> m_doubleMap;

        public DefaultFacetHandlerInitializerParam()
        {
            m_boolMap = new Dictionary<string, bool[]>();
            m_intMap = new Dictionary<string, int[]>();
            m_longMap = new Dictionary<string, long[]>();
            m_stringMap = new Dictionary<string, IList<string>>();
            m_byteMap = new Dictionary<string, byte[]>();
            m_doubleMap = new Dictionary<string, double[]>();
        }

        public override ICollection<string> BooleanParamNames
        {
            get { return m_boolMap.Keys; }
        }

        public override ICollection<string> StringParamNames
        {
            get { return m_stringMap.Keys; }
        }

        public override ICollection<string> IntParamNames
        {
            get { return m_intMap.Keys; }
        }

        public override ICollection<string> ByteArrayParamNames
        {
            get { return m_byteMap.Keys; }
        }

        public override ICollection<string> LongParamNames
        {
            get { return m_longMap.Keys; }
        }

        public override ICollection<string> DoubleParamNames
        {
            get { return m_doubleMap.Keys; }
        }

        public virtual DefaultFacetHandlerInitializerParam PutBooleanParam(string key, bool[] value)
        {
            m_boolMap.Put(key, value);
            return this;
        }

        public override bool[] GetBooleanParam(string name)
        {
            return m_boolMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutByteArrayParam(string key, byte[] value)
        {
            m_byteMap.Put(key, value);
            return this;
        }

        public override byte[] GetByteArrayParam(string name)
        {
            return m_byteMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutIntParam(string key, int[] value)
        {
            m_intMap.Put(key, value);
            return this;
        }

        public override int[] GetIntParam(string name)
        {
            return m_intMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutLongParam(string key, long[] value)
        {
            m_longMap.Put(key, value);
            return this;
        }

        public override long[] GetLongParam(string name)
        {
            return m_longMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutStringParam(string key, IList<string> value)
        {
            m_stringMap.Put(key, value);
            return this;
        }

        public override IEnumerable<string> GetStringParam(string name)
        {
            return m_stringMap.Get(name);
        }

        public virtual DefaultFacetHandlerInitializerParam PutDoubleParam(string key, double[] value)
        {
            m_doubleMap.Put(key, value);
            return this;
        }

        public override double[] GetDoubleParam(string name)
        {
            return m_doubleMap.Get(name);
        }

        public virtual void Clear()
        {
            m_boolMap.Clear();
            m_intMap.Clear();
            m_longMap.Clear();
            m_stringMap.Clear();
            m_byteMap.Clear();
        }
    }
}
