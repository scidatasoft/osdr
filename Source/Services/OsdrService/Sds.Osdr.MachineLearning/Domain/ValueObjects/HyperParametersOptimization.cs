using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.ValueObjects
{
    public class HyperParametersOptimization: ValueObject<HyperParametersOptimization>
    {
        public int NumberOfIterations { get; set; }
        public string OptimizationMethod { get; set; }

        public HyperParametersOptimization(int numberOfIterations, string optimizationMethod)
        {
            NumberOfIterations = numberOfIterations;
            OptimizationMethod = optimizationMethod;
        }

        public HyperParametersOptimization()
        {
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { NumberOfIterations, OptimizationMethod };
        }
    }
}
