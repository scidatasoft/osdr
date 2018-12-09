using Sds.ChemicalProperties.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.ChemicalProperties.WebApi.Dto
{
    public class CalculatedPropertiesResponse
    {
        public Guid Id { get; set; }
        public CalculatedProperties PropertiesCalculationResult { get; set; }
        public Exception Exception { get; set; }
        public DateTime CalculationDate { get; set; }
    }
}

