using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface PredictProperties : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid ParentId { get; }
        Guid DatasetBlobId { get; }
        string DatasetBucket { get; }
        Guid ModelBlobId { get; }
        string ModelBucket { get; }
        Guid UserId { get; }
    }
}
