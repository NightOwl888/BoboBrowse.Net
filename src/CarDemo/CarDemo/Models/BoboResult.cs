using BoboBrowse.Net;
using System.Collections.Generic;
using System.Linq;

namespace CarDemo.Models
{
    public class BoboResult
    {
        public BoboResult(BrowseResult browseResult)
        {
            this.Choices = new Dictionary<string, Choice>();
            this.Hits = new List<Hit>();
            
            // populate the data from the browse result
            this.Time = browseResult.Time;
            this.NumHits = browseResult.NumHits;
            this.TotalDocs = browseResult.TotalDocs;

            foreach (var entry in browseResult.FacetMap)
            {
                string name = entry.Key;
                IEnumerable<BrowseFacet> facets = entry.Value.GetFacets();
                var choiceObject = new Choice();
                //var choiceList = new List<FacetResult>();
                int totalCount = 0;
                foreach (var facet in facets)
                {
                    var choice = new FacetResult();
                    choice.FacetValueHitCount = facet.FacetValueHitCount;
                    choice.Value = facet.Value;
                    choiceObject.ChoiceList.Add(choice);
                    totalCount += facet.FacetValueHitCount;
                }
                choiceObject.TotalCount = totalCount;
                this.Choices.Add(name, choiceObject);
            }

            if (browseResult.Hits != null && browseResult.Hits.Count() > 0)
            {
                foreach (var hit in browseResult.Hits)
                {
                    var ht = new Hit();
                    ht.FieldValues = hit.FieldValues;
                    ht.DocId = hit.DocId;
                    ht.Score = hit.Score;
                    this.Hits.Add(ht);
                }
            }
        }

        public int NumHits { get; private set; }
        public long Time { get; private set; }
        public int TotalDocs { get; private set; }
        public Dictionary<string, Choice> Choices { get; private set; }
        public IList<Hit> Hits { get; private set; }
    }
}