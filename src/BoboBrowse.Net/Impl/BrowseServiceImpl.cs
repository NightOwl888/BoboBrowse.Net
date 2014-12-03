 //* Bobo Browse Engine - High performance faceted/parametric search implementation 
 //* that handles various types of semi-structured data.  Written in Java.
 //* 
 //* Copyright (C) 2005-2006  John Wang
 //*
 //* This library is free software; you can redistribute it and/or
 //* modify it under the terms of the GNU Lesser General Public
 //* License as published by the Free Software Foundation; either
 //* version 2.1 of the License, or (at your option) any later version.
 //*
 //* This library is distributed in the hope that it will be useful,
 //* but WITHOUT ANY WARRANTY; without even the implied warranty of
 //* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 //* Lesser General Public License for more details.
 //*
 //* You should have received a copy of the GNU Lesser General Public
 //* License along with this library; if not, write to the Free Software
 //* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 //* 
 //* To contact the project administrators for the bobo-browse project, 
 //* please go to https://sourceforge.net/projects/bobo-browse/, or 
 //* send mail to owner@browseengine.com. 


namespace BoboBrowse.Net.Impl
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using BoboBrowse.Net;
    using BoboBrowse.Net.Service;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Directory = Lucene.Net.Store.Directory;

    public class BrowseServiceImpl : IBrowseService
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(BrowseServiceImpl));
        private readonly DirectoryInfo idxDir;
        private readonly BoboIndexReader reader;

        public BrowseServiceImpl(DirectoryInfo idxDir)
        {
            this.idxDir = idxDir;
            try
            {
                reader = NewIndexReader();
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

        private BoboIndexReader NewIndexReader()
        {
            Directory idxDir = FSDirectory.Open(this.idxDir);
            return NewIndexReader(idxDir);
        }

        public static BoboIndexReader NewIndexReader(Directory idxDir)
        {
            if (!IndexReader.IndexExists(idxDir))
            {
                return null;
            }

            long start = System.Environment.TickCount;

            IndexReader ir = IndexReader.Open(idxDir, true);
            BoboIndexReader reader;

            try
            {
                reader = BoboIndexReader.GetInstance(ir);
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
        public virtual void close() // throws BrowseException
        {
            try
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
            catch (IOException e)
            {
                throw new BrowseException(e.Message, e);
            }
        }

        public virtual BrowseResult Browse(BrowseRequest req) // throws BrowseException
        {
            return BrowseServiceFactory.CreateBrowseService(reader).Browse(req);
        }
    }
}