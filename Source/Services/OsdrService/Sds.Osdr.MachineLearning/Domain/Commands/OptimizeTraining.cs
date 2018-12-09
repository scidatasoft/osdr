using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface OptimizeTraining : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        string SourceFileName { get; }
        Guid TargetFolderId { get; }
        DateTimeOffset TimeStamp { get; }
        Guid SourceBlobId { get; }
        string SourceBucket { get; }
        IEnumerable<string> Methods { get; }
        string ClassName { get; }
        int DnnLayers { get; }
        int DnnNeurons { get; }
    }
}
