///
/// <summary> * Bobo Browse Engine - High performance faceted/parametric search implementation 
/// * that handles various types of semi-structured data.  Written in Java.
/// * 
/// * Copyright (C) 2005-2006  John Wang
/// *
/// * This library is free software; you can redistribute it and/or
/// * modify it under the terms of the GNU Lesser General Public
/// * License as published by the Free Software Foundation; either
/// * version 2.1 of the License, or (at your option) any later version.
/// *
/// * This library is distributed in the hope that it will be useful,
/// * but WITHOUT ANY WARRANTY; without even the implied warranty of
/// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
/// * Lesser General Public License for more details.
/// *
/// * You should have received a copy of the GNU Lesser General Public
/// * License along with this library; if not, write to the Free Software
/// * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
/// * 
/// * To contact the project administrators for the bobo-browse project, 
/// * please go to https://sourceforge.net/projects/bobo-browse/, or 
/// * send mail to owner@browseengine.com. </summary>
/// 

namespace BoboBrowse.Net.Impl
{
    using System.IO;
    using BoboBrowse.Net;
    using BoboBrowse.Net.Service;
    using Common.Logging;

    public class DefaultBrowseServiceImpl : IBrowseService
    {
        private static ILog logger = LogManager.GetLogger(typeof(DefaultBrowseServiceImpl));
        private BoboIndexReader _reader;
        private bool _closeReader;

        public DefaultBrowseServiceImpl(BoboIndexReader reader)
        {
            _reader = reader;
            _closeReader = false;
        }

        public virtual void setCloseReaderOnCleanup(bool closeReader)
        {
            _closeReader = closeReader;
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
                BoboBrowser browser = new BoboBrowser(_reader);
                result = browser.Browse(req);
            }
            return result;
        }

        public virtual void Close() // throws BrowseException
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
                        catch (IOException ioe)
                        {
                            throw new BrowseException(ioe.Message, ioe);
                        }
                    }
                }
            }
        }
    }
}