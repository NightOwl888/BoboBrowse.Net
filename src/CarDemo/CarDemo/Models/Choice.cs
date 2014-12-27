using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CarDemo.Models
{
    public class Choice
    {
        public Choice()
        {
            this.ChoiceList = new List<FacetResult>();
        }

        public int TotalCount { get; set; }
        public List<FacetResult> ChoiceList { get; set; }
    }
}