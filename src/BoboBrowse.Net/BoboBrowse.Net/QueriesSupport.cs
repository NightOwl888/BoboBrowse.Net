// Version compatibility level: 3.2.0
namespace BoboBrowse.Net
{
    using Lucene.Net.Search;
    using System.Collections.Generic;
    using System.Linq;

    public class QueriesSupport
    {
        public static Lucene.Net.Search.Query CombineAnd(params Lucene.Net.Search.Query[] queries)
        {
            var uniques = new HashSet<Lucene.Net.Search.Query>();
            for (int i = 0; i < queries.Length; i++)
            {
                Lucene.Net.Search.Query query = queries[i];
                List<BooleanClause> clauses = null;
                // check if we can split the query into clauses
                bool splittable = (query is BooleanQuery);
                if (splittable)
                {
                    BooleanQuery bq = (BooleanQuery)query;
                    splittable = bq.IsCoordDisabled();
                    clauses = bq.Clauses;
                    for (int j = 0; splittable && j < clauses.Count; j++)
                    {
                        splittable = (clauses[j].Occur == Occur.MUST);
                    }
                }
                if (splittable)
                {
                    for (int j = 0; j < clauses.Count; j++)
                    {
                        uniques.Add(clauses[j].Query);
                    }
                }
                else
                {
                    uniques.Add(query);
                }
            }
            // optimization: if we have just one query, just return it
            if (uniques.Count == 1)
            {
                return uniques.First();
            }
            BooleanQuery result = new BooleanQuery(true);
            foreach (var query in uniques)
                result.Add(query, Occur.MUST);
            return result;
        }
    }
}
