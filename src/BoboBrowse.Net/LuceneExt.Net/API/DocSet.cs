﻿﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug, Alexey Shcherbachev, and zhengchun.
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

// Kamikaze version compatibility level: 3.0.6
namespace LuceneExt
{
    using Lucene.Net.Search;

    /// <summary>
    /// Represents a sorted integer set
    /// </summary>
    public abstract class DocSet : DocIdSet
    {        
        /// <summary>
        /// Add a doc id to the set 
        /// </summary>
        /// <param name="docid">The doc id to add.</param>
        public abstract void AddDoc(int docid);

        /// <summary>
        /// Add an array of sorted docIds to the set
        /// </summary>
        /// <param name="docids"></param>
        /// <param name="start"></param>
        /// <param name="len"></param>
        public virtual void AddDocs(int[] docids, int start, int len)
        {
            int i = start;
            while (i < len)
            {
                AddDoc(docids[i++]);
            }
        }

        ///<summary>Return the set size </summary>
        ///<returns>true if present, false otherwise </returns>
        public virtual bool Find(int val)
        {
            return FindWithIndex(val) > -1 ? true : false;
        }

        ///<summary>Return the set size </summary>
        ///<returns>index if present, -1 otherwise </returns>
        public virtual int FindWithIndex(int val)
        {
            return -1;
        }

        ///<summary>Gets the number of ids in the set </summary>
        ///<returns>size of the docset </returns>
        public virtual int Size()
        {
            return 0;
        }

        ///<summary>Return the set size in bytes </summary>
        ///<returns>index if present, -1 otherwise </returns>
        public virtual long SizeInBytes()
        {
            return 0;
        }

        ///<summary>Optimize by trimming underlying data structures </summary>
        public virtual void Optimize()
        {
            return;
        }
    }
}
