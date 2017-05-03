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
    using System;

    public class DefaultBrowseServiceImpl : IBrowseService
    {
        private BoboMultiReader m_reader;
        private bool m_closeReader;

        public DefaultBrowseServiceImpl(BoboMultiReader reader)
        {
            m_reader = reader;
            m_closeReader = false;
        }

        public virtual bool CloseReaderOnCleanup
        {
            set { m_closeReader = value; }
        }

        public virtual BrowseResult Browse(BrowseRequest req)
        {
            BrowseResult result = BrowseService.EMPTY_RESULT;
            if (req.Offset < 0)
            {
                throw new BrowseException("Invalid offset: " + req.Offset);
            }
            if (m_reader != null)
            {
                BoboBrowser browser;
                try
                {
                    browser = new BoboBrowser(m_reader);
                }
                catch (Exception e)
                {
                    throw new BrowseException("failed to create BoboBrowser", e);
                }
                result = browser.Browse(req);
            }
            return result;
        }

        public virtual void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_closeReader)
                {
                    lock (this)
                    {
                        if (m_reader != null)
                        {
                            try
                            {
                                m_reader.Dispose();
                                m_reader = null;
                            }
                            catch (Exception ioe)
                            {
                                throw new BrowseException(ioe.Message, ioe);
                            }
                        }
                    }
                }
            }
        }
    }
}