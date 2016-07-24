//* Bobo Browse Engine - High performance faceted/parametric search implementation 
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
namespace BoboBrowse.Net.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Service;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Directory = Lucene.Net.Store.Directory;

    public class BrowseServiceImpl : IBrowseService
    {
        private static readonly ILog logger = LogProvider.For<BrowseServiceImpl>();
        private readonly DirectoryInfo _idxDir;
        private readonly BoboMultiReader _reader;

        public BrowseServiceImpl(DirectoryInfo idxDir)
        {
            this._idxDir = idxDir;
            try
            {
                _reader = NewIndexReader();
            }
            catch (IOException e)
            {
                logger.ErrorException(e.Message, e);
            }
        }

        // FIXME :default impl
        /*public BrowseServiceImpl() : this(new FileInfo(System.getProperty("index.directory")))
        {
        }*/

        private BoboMultiReader NewIndexReader()
        {
            Directory idxDir = FSDirectory.Open(_idxDir);
            return NewIndexReader(idxDir);
        }

        public static BoboMultiReader NewIndexReader(Directory idxDir)
        {
            if (!DirectoryReader.IndexExists(idxDir))
            {
                return null;
            }

            long start = System.Environment.TickCount;

            DirectoryReader directoryReader = DirectoryReader.Open(idxDir);
            BoboMultiReader reader;

            try
            {
                reader = BoboMultiReader.GetInstance(directoryReader);
            }
            catch (IOException ioe)
            {
                throw ioe;
            }
            finally
            {
                directoryReader.Dispose();
            }

            long end = System.Environment.TickCount;

            if (logger.IsDebugEnabled())
            {
                logger.Debug("New index loading took: " + (end - start));
            }

            return reader;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                }
            }
        }

        public virtual BrowseResult Browse(BrowseRequest req) // throws BrowseException
        {
            return BrowseServiceFactory.CreateBrowseService(_reader).Browse(req);
        }
    }
}