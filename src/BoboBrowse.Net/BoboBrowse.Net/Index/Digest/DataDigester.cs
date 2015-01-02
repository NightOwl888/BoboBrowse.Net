/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  John Wang
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */

// Version compatibility level: 3.1.0
namespace BoboBrowse.Net.Index.Digest
{
    using BoboBrowse.Net.Facets;
    using Lucene.Net.Documents;
    using System.Collections.Generic;
    using System.Text;

    public abstract class DataDigester
    {
        public interface IDataHandler
        {
            void HandleDocument(Document doc);
        }

        protected DataDigester()
        {
        }

        public static void PopulateDocument(Document doc, IEnumerable<IFacetHandler> handlers)
        {
            StringBuilder tokenBuffer = new StringBuilder();

            foreach (var handler in handlers)
            {
                string name = handler.Name;
                string[] values = doc.GetValues(name);
                if (values != null)
                {
                    doc.RemoveFields(name);
                    foreach (string value in values)
                    {
                        doc.Add(new Field(name, value, Field.Store.NO, Field.Index.NOT_ANALYZED));
                        tokenBuffer.Append(' ').Append(value);
                    }
                }
            }
        }

        public abstract void Digest(IDataHandler handler);
    }
}
