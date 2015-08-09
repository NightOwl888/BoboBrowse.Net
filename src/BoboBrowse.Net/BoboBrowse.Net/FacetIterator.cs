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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Support;

    /// <summary>
    /// Iterator to iterate over facets
    /// author nnarkhed
    /// </summary>
    public abstract class FacetIterator : IIterator<string>
    {
        protected int count;
        protected string facet;

        public virtual int Count 
        { 
            get { return count; } 
        }

        public virtual string Facet
        {
            get { return facet; }
        }

        /// <summary>
        /// Moves the iteration to the next facet
        /// </summary>
        /// <returns>The next facet value.</returns>
        public abstract string Next();

        /// <summary>
        /// Moves the iteration to the next facet whose hitcount >= minHits. returns null if there is no facet whose hitcount >= minHits.
        /// Hence while using this method, it is useless to use hasNext() with it.
        /// After the next() method returns null, calling it repeatedly would result in undefined behavior 
        /// </summary>
        /// <param name="minHits"></param>
        /// <returns>The next facet value. It returns null if there is no facet whose hitcount >= minHits.</returns>
        public abstract string Next(int minHits);

        public abstract string Format(object val);


        // From IIterator<E>

        public abstract bool HasNext();

        public abstract void Remove();
    }
}
