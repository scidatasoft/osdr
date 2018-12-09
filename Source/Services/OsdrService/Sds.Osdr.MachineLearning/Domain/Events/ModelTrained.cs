using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ModelTrained : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
        int NumberOfGenericFiles { get; }
        IDictionary<string, object> Metadata { get; }
        string DisplayMethodName { get; }
        string PropertyName { get; }
        string PropertyCategory { get; }
        string PropertyUnits { get; }
        string PropertyDescription { get; }
        string DatasetTitle { get; }
        string DatasetDescription { get; }
        double Modi { get; }
    }
}
