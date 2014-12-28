using System.Collections.Generic;

namespace CarDemo.Models
{
    public class BoboRequest
    {
        public BoboRequest()
        {
            this.Selections = new List<Selection>();
            this.Facets = new List<Facet>();
        }

        public string Query { get; set; } // Search query
        public string Df { get; set; } // Default

        public string Sort { get; set; } // Sort string
        public int Start { get; set; } // offset
        public int Rows { get; set; } // count
        public bool Facet { get; set; }
        public List<Selection> Selections { get; set; }
        public List<Facet> Facets { get; set; }
    }
}