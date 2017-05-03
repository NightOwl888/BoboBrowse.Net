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
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.QueryParsers.Classic;
    using Lucene.Net.Search;
    using Lucene.Net.Util;

    public class QueryProducer
    {
        public const string CONTENT_FIELD = "contents";

        public static Query Convert(string queryString, string defaultField)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return null;
            }
            else
            {
                var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
                if (string.IsNullOrEmpty(defaultField)) defaultField = "contents";
                return new QueryParser(LuceneVersion.LUCENE_48, defaultField, analyzer).Parse(queryString);
            }
        }

        private readonly static SortField[] DEFAULT_SORT = new SortField[] { SortField.FIELD_SCORE };

        public virtual Query BuildQuery(string query)
        {
            return Convert(query, CONTENT_FIELD);
        }
    }
}
