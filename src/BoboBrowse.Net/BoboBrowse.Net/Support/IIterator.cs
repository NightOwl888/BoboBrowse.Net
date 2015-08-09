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
    /// <summary>
    /// An iterator over a collection. Iterator takes the place of Enumeration in the Java Collections Framework. Iterators differ from enumerations in two ways:
    /// <list type="">
    ///     <item>Iterators allow the caller to remove elements from the underlying collection during the iteration with well-defined semantics.</item>
    ///     <item>Method names have been improved.</item>
    /// </list>
    ///     
    /// This interface is a member of the Java Collections Framework.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    public interface IIterator<E>
    {
        /// <summary>
        /// Returns true if the iteration has more elements.
        /// </summary>
        /// <returns></returns>
        bool HasNext();

        /// <summary>
        /// Returns the next element in the iteration.
        /// </summary>
        /// <returns></returns>
        E Next();

        /// <summary>
        /// Removes from the underlying collection the last element returned by this iterator (optional operation).
        /// </summary>
        void Remove();
    }
}
