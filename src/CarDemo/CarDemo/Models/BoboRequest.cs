using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BoboBrowse.Net;

namespace CarDemo.Models
{
    public class BoboRequest
    {
        public BoboRequest()
        {
            //this.Fields = new List<Field>();

            //this.SortFields = new List<SortField>();
            this.Selections = new List<Selection>();
            this.Facets = new List<Facet>();
        }

        public string Query { get; set; } // Search query
        public string Df { get; set; } // Default

        public string Sort { get; set; } // Sort string
        //public FacetSpec.FacetSortSpec SortDirection { get; set; }
        //public List<SortField> SortFields { get; set; }
        public int Start { get; set; } // offset
        public int Rows { get; set; } // count
        public bool Facet { get; set; }
        public List<Selection> Selections { get; set; }
        public List<Facet> Facets { get; set; }


        //public List<Field> Fields { get; set; }
    }
}