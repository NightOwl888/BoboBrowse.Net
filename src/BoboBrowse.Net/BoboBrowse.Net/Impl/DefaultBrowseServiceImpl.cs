// /**
// * Bobo Browse Engine - High performance faceted/parametric search implementation 
// * that handles various types of semi-structured data.  Written in Java.
// * 
// * Copyright (C) 2005-2006  John Wang
// *
// * This library is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 2.1 of the License, or (at your option) any later version.
// *
// * This library is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public
// * License along with this library; if not, write to the Free Software
// * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// * 
// * To contact the project administrators for the bobo-browse project, 
// * please go to https://sourceforge.net/projects/bobo-browse/, or 
// * send mail to owner@browseengine.com.
// */

// Version compatibility level: 3.2.0
namespace BoboBrowse.Net.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Service;
    using Common.Logging;
    using System;

    public class DefaultBrowseServiceImpl : IBrowseService
    {
        private static ILog logger = LogManager.GetLogger<DefaultBrowseServiceImpl>();
        private BoboIndexReader _reader;
        private bool _closeReader;

        public DefaultBrowseServiceImpl(BoboIndexReader reader)
        {
            _reader = reader;
            _closeReader = false;
        }

        public virtual bool CloseReaderOnCleanup
        {
            set { _closeReader = value; }
        }

        public virtual BrowseResult Browse(BrowseRequest req) // throws BrowseException
        {
            BrowseResult result = BrowseService_Fields.EMPTY_RESULT;
            if (req.Offset < 0)
            {
                throw new BrowseException("Invalid offset: " + req.Offset);
            }
            if (_reader != null)
            {
                BoboBrowser browser;
                try
                {
                    browser = new BoboBrowser(_reader);
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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_closeReader)
                {
                    lock (this)
                    {
                        if (_reader != null)
                        {
                            try
                            {
                                _reader.Dispose();
                                _reader = null;
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