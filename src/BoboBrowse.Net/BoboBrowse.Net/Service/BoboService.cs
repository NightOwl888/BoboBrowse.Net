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
namespace BoboBrowse.Net.Service
{
    using BoboBrowse.Net;
    using Common.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System;
    using System.IO;

    public class BoboService
    {
        private static ILog logger = LogManager.GetLogger(typeof(BoboService));

        private readonly DirectoryInfo _idxDir;
        private BoboIndexReader _boboReader;

        public BoboService(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public BoboService(DirectoryInfo idxDir)
        {
            this._idxDir = idxDir;
            _boboReader = null;
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BoboBrowser browser = null;
            try
            {
                browser = new BoboBrowser(_boboReader);
                return browser.Browse(req);
            }
            catch (Exception e)
            {
                logger.Error(e.Message, e);
                return new BrowseResult();
            }
            finally
            {
                if (browser != null)
                {
                    try
                    {
                        browser.Dispose();
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message);
                    }
                }
            }
        }

        public virtual void Start()
        {
            IndexReader reader = IndexReader.Open(FSDirectory.Open(_idxDir), true);
            try
            {
                _boboReader = BoboIndexReader.GetInstance(reader);
            }
            catch
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }
        }

        public virtual void Shutdown()
        {
            if (_boboReader != null)
            {
                try
                {
                    _boboReader.Dispose();
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}