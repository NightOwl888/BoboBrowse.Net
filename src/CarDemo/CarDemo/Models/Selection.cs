using BoboBrowse.Net;
using System.Collections.Generic;

namespace CarDemo.Models
{
    public class Selection
    {
        public Selection()
        {
            this.Values = new List<string>();
        }

        public string Name { get; set; }
        public int Depth { get; set; }
        public bool Strict { get; set; }
        public List<string> Values { get; set; }
        public BrowseSelection.ValueOperation SelectionOperation { get; set; }
    }
}