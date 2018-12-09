using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface SaveTrainedModel : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        string Message { get; }
        Guid ParentId { get; }
        Guid ModelBlobId { get; }
        string ModelBucket { get; }
        string ModelName { get; }
        string SourceFileName { get; }
        Guid SourceBlobId { get; }
        string SourceBucket { get; }
        string UserName { get; }
        string Method { get; }
        string ClassName { get; }
        string ValueName { get; }
        string ValueUnits { get; }
        decimal? CutOffValue { get; }
        string RelationProperty { get; }
        decimal SubSampleSize { get; }
        decimal TestDatasetSize { get; }
        int KFold { get; }
        int FingerprintRadius { get; }
        int FingerprintSize { get; }
        FingerprintType FingerprintType { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
