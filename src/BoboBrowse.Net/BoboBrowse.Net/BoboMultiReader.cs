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
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Support;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Reflection;

    public class BoboMultiReader : FilterDirectoryReader
    {
        protected IEnumerable<BoboSegmentReader> _subReaders = new List<BoboSegmentReader>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Directory reader</param>
        /// <returns>A new BoboMultiReader instance.</returns>
        public static BoboMultiReader GetInstance(DirectoryReader reader) 
        {
            return BoboMultiReader.GetInstance(reader, null);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">Directory reader</param>
        /// <param name="facetHandlers">List of facet handlers</param>
        /// <returns>A new BoboMultiReader instance.</returns>
        public static BoboMultiReader GetInstance(DirectoryReader reader, IEnumerable<IFacetHandler> facetHandlers) 
        {
            BoboMultiReader boboReader = new BoboMultiReader(reader, facetHandlers);
            boboReader.FacetInit();
            return boboReader;
        }

        protected override void DoClose()
        {
            // do nothing
        }

        protected BoboMultiReader(DirectoryReader reader, IEnumerable<IFacetHandler> facetHandlers)
            : base(reader, new BoboSubReaderWrapper(reader, facetHandlers))
        {
            _subReaders = GetSequentialSubReaders().Cast<BoboSegmentReader>().ToList();
        }

        protected void FacetInit()
        {
            foreach (BoboSegmentReader r in _subReaders)
            {
                r.FacetInit();
            }
        }

        public IEnumerable<BoboSegmentReader> GetSubReaders()
        {
            return _subReaders;
        }

        public int SubReaderBase(int readerIndex)
        {
            return ReaderBase(readerIndex);
        }

        public class BoboSubReaderWrapper : SubReaderWrapper
        {
            private const string SPRING_CONFIG = "bobo.spring";
            private readonly BoboSegmentReader.WorkArea _workArea = new BoboSegmentReader.WorkArea();
            private IEnumerable<IFacetHandler> _facetHandlers = null;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="facetHandlers"></param>
            public BoboSubReaderWrapper(DirectoryReader reader, IEnumerable<IFacetHandler> facetHandlers)
            {
                // NOTE: Spring support was removed in Bobo 4.0.2 in Java, but we are still including it in 
                // the .NET version.
                if (facetHandlers == null)
                {
                    var idxDir = reader.Directory();
                    if (idxDir != null && idxDir is FSDirectory)
                    {
                        // Look for the bobo.spring file in the same directory as the Lucene index
                        var dir = ((FSDirectory)idxDir).Directory;
                        var springConfigFile = Path.Combine(dir.FullName, SPRING_CONFIG);
                        Type loaderType = Type.GetType("BoboBrowse.Net.Spring.FacetHandlerLoader, BoboBrowse.Net.Spring");

                        if (loaderType != null)
                        {
                            var loaderInstance = Activator.CreateInstance(loaderType);

                            MethodInfo methodInfo = loaderType.GetMethod("LoadFacetHandlers");
                            facetHandlers = (IEnumerable<IFacetHandler>)methodInfo.Invoke(loaderInstance, new object[] { springConfigFile, _workArea });
                        }
                        else if (File.Exists(springConfigFile))
                        {
                            throw new RuntimeException(string.Format(
                                "There is a file named '{0}' in the Lucene.Net index directory '{1}', but you don't have " +
                                "the BoboBrowse.Net.Spring assembly in your project to resolve the references. You can " +
                                "download BoboBrowse.Net.Spring as a separate optional package from NuGet or you can provide " +
                                "facet handlers using an alternate BoboBrowseIndex.GetInstance overload", SPRING_CONFIG, dir));
                        }
                        else
                        {
                            facetHandlers = new List<IFacetHandler>();
                        }
                    }
                    else
                    {
                        facetHandlers = new List<IFacetHandler>();
                    }
                }

                _facetHandlers = facetHandlers;
            }

            public override AtomicReader Wrap(AtomicReader reader)
            {
                return new BoboSegmentReader(reader, _facetHandlers, null, _workArea);
            }
        }

        protected override DirectoryReader DoWrapDirectoryReader(DirectoryReader @in)
        {
            return @in;
        }
    }
}
