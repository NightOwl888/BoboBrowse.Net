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
namespace BoboBrowse.Net
{
    /// <summary>
    /// NOTE: This was FloatFacetIterator in bobo-browse
    /// <para/>
    /// author "Xiaoyang Gu &lt;xgu@linkedin.com&gt;"
    /// </summary>
    public abstract class SingleFacetIterator : FacetIterator
    {
        new protected float m_facet;

        new public virtual float Facet
        {
            get { return m_facet; }
        }

        /// <summary>
        /// NOTE: This was NextFloat() in bobo-browse
        /// </summary>
        public abstract float NextSingle();

        /// <summary>
        /// NOTE: This was NextFloat() in bobo-browse
        /// </summary>
        public abstract float NextSingle(int minHits);

        public abstract string Format(float val);
    }
}
