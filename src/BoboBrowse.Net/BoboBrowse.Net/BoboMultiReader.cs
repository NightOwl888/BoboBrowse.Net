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
    using Lucene.Net.Index;
    using System.Collections.Generic;
    using System.Linq;

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
            : base(reader, new BoboSubReaderWrapper(facetHandlers))
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

            private readonly BoboSegmentReader.WorkArea workArea = new BoboSegmentReader.WorkArea();
            private IEnumerable<IFacetHandler> _facetHandlers = null;

            /** Constructor */
            public BoboSubReaderWrapper(IEnumerable<IFacetHandler> facetHandlers)
            {
                _facetHandlers = facetHandlers;
            }

            public override AtomicReader Wrap(AtomicReader reader)
            {
                return new BoboSegmentReader(reader, _facetHandlers, null, workArea);
            }
        }

        protected override DirectoryReader DoWrapDirectoryReader(DirectoryReader @in)
        {
            return @in;
        }
    }
}
