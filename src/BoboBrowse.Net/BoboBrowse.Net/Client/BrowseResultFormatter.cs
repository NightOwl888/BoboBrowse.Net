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
