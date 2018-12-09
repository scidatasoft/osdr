using Sds.ChemicalStandardizationValidation.Domain.Models;
using System;
using System.Collections.Generic;

namespace Sds.ChemicalStandardizationValidation.Domain.Models
{
    public class StandardizedRecord
    {
        public Guid StandardizedId { get; set; }
        public List<Issue> Issues { get; set; }
    }
}
