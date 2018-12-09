using Sds.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.ValueObjects
{
    public class Property : ValueObject<Property>
    {
        //public string Code { get; private set; }
        public string Category { get; private set; }
        public string Name { get; private set; }
        public string Units { get; private set; }
        public string Description { get; private set; }

        public Property(
            //string code, 
            string category, string name, string units, string description)
        {
            //Code = code;
            Category = category;
            Name = name;
            Units = units;
            Description = description;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { Category, Name, Units, Description };
        }
    }
}
