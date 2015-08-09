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

namespace BoboBrowse.Net.Client
{
    using System.Text;

    public class BrowseResultFormatter
    {
        public static string FormatResults(BrowseResult res)
        {
            var sb = new StringBuilder();
            sb.Append(res.NumHits);
            sb.Append(" hits out of ");
            sb.Append(res.TotalDocs);
            sb.Append(" docs\n");
            BrowseHit[] hits = res.Hits;
            var map = res.FacetMap;
            var keys = map.Keys;
            foreach (string key in keys) 
            {
                var fa = map[key];
                sb.AppendLine(key);
                var lf = fa.GetFacets();
                foreach (var bf in lf) 
                {
                    sb.AppendLine("\t" + bf);
                }
            }
            foreach (BrowseHit hit in hits) 
            {
                sb.AppendLine("------------");
                sb.Append(FormatHit(hit));
                sb.AppendLine();
            }
            sb.Append("*****************************\n");
            return sb.ToString();
        }

        private static string FormatHit(BrowseHit hit)
        {
            var sb = new StringBuilder();
            var fields = hit.FieldValues;
            var keys = fields.Keys;
            foreach (string key in keys)
            {
                sb.Append("\t" + key + " :");
                string[] values = fields[key];
                foreach (var value in values)
                {
                    sb.Append(" " + value);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
