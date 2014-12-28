using BoboBrowse.Net.Impl;
using Lucene.Net.Search;
using System;
using System.Text.RegularExpressions;

namespace CarDemo.BoboServices
{
    public class BoboDefaultQueryBuilder
    {
        private static Regex sortSep = new Regex(",", RegexOptions.Compiled);

        public Query ParseQuery(string query, string defaultField)
        {
            try
            {
                return QueryProducer.Convert(query, defaultField);
            }
            catch
            {
                return null;
            }
        }

        public Sort ParseSort(string sortSpec)
        {
            if (sortSpec == null || sortSpec.Length == 0) return null;

            string[] parts = sortSep.Split(sortSpec.Trim());
            if (parts.Length == 0) return null;

            SortField[] lst = new SortField[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                bool top = true;

                int idx = part.IndexOf(' ');
                if (idx > 0)
                {
                    string order = part.Substring(idx + 1).Trim();
                    if ("desc".Equals(order) || "top".Equals(order))
                    {
                        top = true;
                    }
                    else if ("asc".Equals(order) || "bottom".Equals(order))
                    {
                        top = false;
                    }
                    else
                    {
                        throw new ArgumentException("Unknown sort order: " + order);
                    }
                    part = part.Substring(0, idx).Trim();
                }
                else
                {
                    throw new ArgumentException("Missing sort order.");
                }

                if ("score".Equals(part))
                {
                    if (top)
                    {
                        // If thre is only one thing in the list, just do the regular thing...
                        if (parts.Length == 1)
                        {
                            return null; // do normal scoring...
                        }
                        lst[i] = SortField.FIELD_SCORE;
                    }
                    else
                    {
                        lst[i] = new SortField(null, SortField.SCORE, true);
                    }
                }
                else
                {
                    lst[i] = new SortField(part, SortField.STRING, top);
                }
            }
            return new Sort(lst);
        }
    }
}