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

namespace BoboBrowse.Net.Service
{
    using System.IO;
    using BoboBrowse.Net.Impl;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using Directory = Lucene.Net.Store.Directory;

    public class BrowseServiceFactory
    {
        private static ILog logger = LogManager.GetLogger(typeof(BrowseServiceFactory));

        public static IBrowseService CreateBrowseService(DirectoryInfo idxDir)
        {
            if (idxDir == null)
            {
                throw new System.ArgumentException("Null index dir specified");
            }
            return new BrowseServiceImpl(idxDir);
        }

        public static IBrowseService CreateBrowseService(BoboIndexReader bReader)
        {
            return new DefaultBrowseServiceImpl(bReader);
        }

        public static BoboIndexReader GetBoboIndexReader(Directory idxDir)
        {
            try
            {
                if (!BoboIndexReader.IndexExists(idxDir))
                {
                    throw new BrowseException("Index does not exist at: " + idxDir);
                }
            }
            catch (IOException ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }

            IndexReader reader = null;
            try
            {
                reader = IndexReader.Open(idxDir, true);
            }
            catch (IOException ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }

            BoboIndexReader bReader = null;
            try
            {
                bReader = BoboIndexReader.GetInstance(reader);
            }
            catch (IOException ioe)
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Dispose();
                    }
                    catch (IOException e)
                    {
                        logger.Error(e.Message, e);
                    }
                }
                throw new BrowseException(ioe.Message, ioe);
            }
            return bReader;
        }

        public static IBrowseService CreateBrowseService(Directory idxDir) // throws BrowseException
        {
            BoboIndexReader bReader = GetBoboIndexReader(idxDir);

            DefaultBrowseServiceImpl bs = (DefaultBrowseServiceImpl)CreateBrowseService(bReader);
            bs.CloseReaderOnCleanup = true;

            return bs;
        }
    }
}