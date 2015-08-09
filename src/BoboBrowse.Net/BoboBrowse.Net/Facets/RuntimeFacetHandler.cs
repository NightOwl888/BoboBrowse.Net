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
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public interface IRuntimeFacetHandler : IFacetHandler, IDisposable
    {
    }

    /// <summary>
    /// Abstract class for RuntimeFacetHandlers. A concrete RuntimeFacetHandler should implement
    /// the FacetHandlerFactory and RuntimeInitializable so that bobo knows how to create new
    /// instance of the handler at run time and how to initialize it at run time respectively.
    /// 
    /// author ymatsuda
    /// </summary>
    /// <typeparam name="D">type parameter for FacetData</typeparam>
    public abstract class RuntimeFacetHandler<D> : FacetHandler<D>, IRuntimeFacetHandler
    {
        /// <summary>
        /// Constructor that specifying the dependent facet handlers using names.
        /// </summary>
        /// <param name="name">the name of this FacetHandler, which is used in FacetSpec and 
        /// Selection to specify the facet. If we regard a facet as a field, the name is like a field name.</param>
        /// <param name="dependsOn">Set of names of facet handlers this facet handler depend on for loading.</param>
        public RuntimeFacetHandler(string name, IEnumerable<string> dependsOn)
            : base(name, dependsOn)
        {}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">the name of this FacetHandler, which is used in FacetSpec and Selection to specify
        /// the facet. If we regard a facet as a field, the name is like a field name.</param>
        public RuntimeFacetHandler(string name)
            : base(name)
        { }

        public override T GetFacetData<T>(BoboIndexReader reader)
        {
            return (T)reader.GetRuntimeFacetData(_name);
        }

        public override void LoadFacetData(BoboIndexReader reader)
        {
            reader.PutRuntimeFacetData(_name, Load(reader));
            reader.PutRuntimeFacetData(_name, this);
        }

        public virtual void Dispose()
        {
        }
    }
}
