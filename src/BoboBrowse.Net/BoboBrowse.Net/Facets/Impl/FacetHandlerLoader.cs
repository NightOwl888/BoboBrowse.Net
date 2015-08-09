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
namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    // NOTE: This type doesn't appear to be used anywhere (or complete).

    //public class FacetHandlerLoader
    //{
    //    private FacetHandlerLoader()
    //    {

    //    }
    //    public static void Load(IEnumerable<IFacetHandler> tobeLoaded)
    //    {
    //        Load(tobeLoaded, null);
    //    }

    //    public static void Load(IEnumerable<IFacetHandler> tobeLoaded, IDictionary<string, IFacetHandler> preloaded)
    //    {

    //    }

    //    private static void Load(BoboIndexReader reader, IEnumerable<IFacetHandler> tobeLoaded, IDictionary<string, IFacetHandler> preloaded, IEnumerable<string> visited)
    //    {
    //        IDictionary<string, IFacetHandler> loaded = new Dictionary<string, IFacetHandler>();
    //        if (preloaded != null)
    //        {
    //            loaded.PutAll(preloaded);
    //        }

    //        IEnumerator<IFacetHandler> iter = tobeLoaded.GetEnumerator();

    //        while (iter.MoveNext())
    //        {
    //            IFacetHandler handler = iter.Current;
    //            if (!loaded.ContainsKey(handler.Name))
    //            {
    //                IEnumerable<string> depends = handler.DependsOn;
    //                if (depends.Count() > 0)
    //                {
    //                }
    //                handler.Load(reader);
    //            }
    //        }
    //    }
    //}
}
