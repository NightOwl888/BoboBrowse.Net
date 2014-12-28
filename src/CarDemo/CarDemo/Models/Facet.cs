namespace CarDemo.Models
{
    public class Facet
    {
        public Facet()
        {
            MinCount = int.MinValue;
            Limit = int.MinValue;
        }

        public string Name { get; set; }
        public int Limit { get; set; } // MinHitCount
        public int MinCount { get; set; } // MaxCount
        public bool Expand { get; set; } // ExpandSelection
        public string Sort { get; set; } // OrderBy
    }
}