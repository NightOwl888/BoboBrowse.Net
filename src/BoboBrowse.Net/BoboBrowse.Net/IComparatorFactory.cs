﻿//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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

// Version compatibility level: 4.0.2
namespace BoboBrowse.Net
{
    using BoboBrowse.Net.Util;
    using System.Collections.Generic;

    /// <summary>
    /// Comparer for custom sorting a facet value.
    /// author jwang
    /// </summary>
    public interface IComparerFactory
    {
        ///<summary>Providers a Comparer from field values and counts. This is called within a browse. </summary>
        ///<param name="fieldValueAccessor"> accessor for field values </param>
        ///<param name="counts"> hit counts </param>
        ///<returns> Comparer instance </returns>
        IComparer<int> NewComparer(IFieldValueAccessor fieldValueAccessor, BigSegmentedArray counts);

        ///<summary>Providers a Comparer. This is called when doing a merge across browses. </summary>
        ///<returns> Comparer instance </returns>
        IComparer<BrowseFacet> NewComparer();
    }
}
