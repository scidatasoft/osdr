using Sds.Domain;
using System.Collections.Generic;

namespace Sds.ChemicalProperties.Domain.Models
{
    public class CalculatedProperties
    {
        public IEnumerable<Property> Properties { get; set; }
        public IEnumerable<Issue> Issues { get; set; }
    }
}
