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
    using BoboBrowse.Net;
    using BoboBrowse.Net.Support.Logging;
    using Lucene.Net.Index;
    using Lucene.Net.Store;
    using System;
    using System.IO;

    public class BoboService
    {
        private static readonly ILog logger = LogProvider.For<BoboService>();

        private readonly DirectoryInfo m_idxDir;
        private BoboMultiReader m_boboReader;

        public BoboService(string path)
            : this(new DirectoryInfo(path))
        {
        }

        public BoboService(DirectoryInfo idxDir)
        {
            this.m_idxDir = idxDir;
            m_boboReader = null;
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BoboBrowser browser = null;
            try
            {
                browser = new BoboBrowser(m_boboReader);
                return browser.Browse(req);
            }
            catch (Exception e)
            {
                logger.ErrorException(e.Message, e);
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
            DirectoryReader reader = DirectoryReader.Open(FSDirectory.Open(m_idxDir));
            try
            {
                m_boboReader = BoboMultiReader.GetInstance(reader);
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
            if (m_boboReader != null)
            {
                try
                {
                    m_boboReader.Dispose();
                }
                catch (Exception e)
                {
                    logger.Error(e.Message);
                }
            }
        }
    }
}