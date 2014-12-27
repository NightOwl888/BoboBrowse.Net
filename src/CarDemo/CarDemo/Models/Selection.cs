using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BoboBrowse.Net;

namespace CarDemo.Models
{
    public class Selection
    {
        public Selection()
        {
            //this.SelectionProperties = new List<SelectionProperty>();
            //this.FacetQueries = new List<FacetQuery>();
            this.Values = new List<string>();
        }

        public string Name { get; set; }
        public int Depth { get; set; }
        public bool Strict { get; set; }
        public List<string> Values { get; set; }
        //public List<FacetQuery> FacetQueries { get; set; }
        //public int DefaultLimit { get; set; }
        //public int DefaultMinCount { get; set; }
        //public string DefaultFacetSort { get; set; }
        //public List<SelectionProperty> SelectionProperties { get; set; }
        //public List<SelectionOperation> SelectionOperations { get; set; }
        public BrowseSelection.ValueOperation SelectionOperation { get; set; }
    }
}