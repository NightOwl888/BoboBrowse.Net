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

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Service;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Directory = Lucene.Net.Store.Directory;

    public class BrowseServiceImpl : IBrowseService
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(BrowseServiceImpl));
        private readonly DirectoryInfo _idxDir;
        private readonly BoboSegmentReader _reader;

        public BrowseServiceImpl(DirectoryInfo idxDir)
        {
            this._idxDir = idxDir;
            try
            {
                _reader = NewIndexReader();
            }
            catch (IOException e)
            {
                logger.Error(e.Message, e);
            }
        }

        // FIXME :default impl
        /*public BrowseServiceImpl() : this(new FileInfo(System.getProperty("index.directory")))
        {
        }*/

        private BoboSegmentReader NewIndexReader()
        {
            Directory idxDir = FSDirectory.Open(_idxDir);
            return NewIndexReader(idxDir);
        }

        public static BoboSegmentReader NewIndexReader(Directory idxDir)
        {
            if (!IndexReader.IndexExists(idxDir))
            {
                return null;
            }

            long start = System.Environment.TickCount;

            IndexReader ir = IndexReader.Open(idxDir, true);
            BoboSegmentReader reader;

            try
            {
                reader = BoboSegmentReader.GetInstance(ir);
            }
            catch (IOException ioe)
            {
                try
                {
                    ir.Dispose();
                }
                catch
                {
                }
                throw ioe;
            }

            long end = System.Environment.TickCount;

            if (logger.IsDebugEnabled)
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