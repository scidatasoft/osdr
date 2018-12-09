using Sds.ChemicalStandardizationValidation.Domain.Models;
using System.Collections.Generic;

namespace Sds.ChemicalStandardizationValidation.Domain.Models
{
    public class ValidatedRecord
    {
        public IEnumerable<Issue> Issues { get; set; }
    }
}
