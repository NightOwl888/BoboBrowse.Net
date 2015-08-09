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
namespace BoboBrowse.Net.Facets
{
    using System;

    /// <summary>
    /// This interface is intended for using with RuntimeFacetHandler, which typically
    /// have local data that make them not only NOT thread safe but also dependent on
    /// request. So it is necessary to have different instance for different client or
    /// request. Typically, the new instance need to be initialized before use.
    /// 
    /// author xiaoyang
    /// </summary>
    public interface IRuntimeFacetHandlerFactory
    {
        /// <summary>
        /// Gets the facet name of the RuntimeFacetHandler it creates.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets if this facet support empty params or not.
        /// </summary>
        bool IsLoadLazily { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="params">the data used to initialize the RuntimeFacetHandler.</param>
        /// <returns>a new instance of </returns>
        IRuntimeFacetHandler Get(FacetHandlerInitializerParam @params);
    }
}
