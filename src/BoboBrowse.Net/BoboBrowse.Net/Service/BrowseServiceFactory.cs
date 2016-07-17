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
namespace BoboBrowse.Net.Service
{
    using BoboBrowse.Net.Impl;
    using Common.Logging;
    using Lucene.Net.Index;
    using System;
    using System.IO;
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

        public static IBrowseService CreateBrowseService(BoboMultiReader bReader)
        {
            return new DefaultBrowseServiceImpl(bReader);
        }

        public static BoboMultiReader GetBoboIndexReader(Directory idxDir)
        {
            try
            {
                if (!BoboMultiReader.IndexExists(idxDir))
                {
                    throw new BrowseException("Index does not exist at: " + idxDir);
                }
            }
            catch (Exception ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }

            DirectoryReader reader = null;
            try
            {
                reader = DirectoryReader.Open(idxDir);
            }
            catch (Exception ioe)
            {
                throw new BrowseException(ioe.Message, ioe);
            }

            BoboMultiReader bReader = null;
            try
            {
                bReader = BoboMultiReader.GetInstance(reader);
            }
            catch (Exception ioe)
            {
                if (reader != null)
                {
                    try
                    {
                        reader.Dispose();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message, e);
                    }
                }
                throw new BrowseException(ioe.Message, ioe);
            }
            return bReader;
        }

        public static IBrowseService CreateBrowseService(Directory idxDir)
        {
            BoboMultiReader bReader = GetBoboIndexReader(idxDir);

            DefaultBrowseServiceImpl bs = (DefaultBrowseServiceImpl)CreateBrowseService(bReader);
            bs.CloseReaderOnCleanup = true;

            return bs;
        }
    }
}