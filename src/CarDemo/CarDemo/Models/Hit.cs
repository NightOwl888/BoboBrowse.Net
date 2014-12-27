using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CarDemo.Models
{
    public class Hit
    {
        public Hit()
        {
            this.FieldValues = new Dictionary<string, string[]>();
        }

        public IDictionary<string, string[]> FieldValues { get; set; }
        public int DocId { get; set; }
        public float Score { get; set; }
    }
}