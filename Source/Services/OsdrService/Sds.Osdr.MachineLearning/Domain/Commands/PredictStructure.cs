using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public class PredictStructure : CorrelatedBy<Guid>
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public string Structure { get; set; }
        public string Format { get; set; }
        public string PropertyName { get; set; }
        public IEnumerable<IDictionary<string, object>> Models { get; set; }

        public PredictStructure(Guid id, Guid correlationId, string structure, string format, string propertyName, IEnumerable<IDictionary<string, object>> models)
        {
            Id = id;
            CorrelationId = correlationId;
            Structure = structure;
            Format = format;
            PropertyName = propertyName;
            Models = models;
        }
    }
}
