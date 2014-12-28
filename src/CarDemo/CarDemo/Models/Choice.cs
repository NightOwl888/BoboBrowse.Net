using System.Collections.Generic;

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