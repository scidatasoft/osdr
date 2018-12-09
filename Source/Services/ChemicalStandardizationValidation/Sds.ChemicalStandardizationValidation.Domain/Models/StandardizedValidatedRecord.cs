using System;
using System.Collections.Generic;

namespace Sds.ChemicalStandardizationValidation.Domain.Models
{
    public class StandardizedValidatedRecord
    {
        public Guid? StandardizedId { get; set; }
        public List<Issue> Issues { get; set; }
    }
}
