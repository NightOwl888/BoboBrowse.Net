//* Bobo Browse Engine - High performance faceted/parametric search implementation 
//* that handles various types of semi-structured data.  Originally written in Java.
//*
//* Ported and adapted for C# by Shad Storhaug.
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
