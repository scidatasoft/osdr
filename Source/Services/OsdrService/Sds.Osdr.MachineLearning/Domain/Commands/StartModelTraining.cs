using MassTransit;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface StartModelTraining : CorrelatedBy<Guid>
	{
        Guid SourceBlobId { get; }
        string Scaler { get; }
        string SourceBucket { get; }
        Guid FolderId { get; } 
        string Method { get; }
        string ClassName { get; }
        decimal SubSampleSize { get; }
        decimal TestDatasetSize { get; }
        int KFold { get; }
        IEnumerable<IDictionary<string, object>> Fingerprints { get; }
		Guid Id { get; }
		Guid UserId { get; }
        HyperParametersOptimization HyperParameters { get; }
        int DnnLayers { get; }
        int DnnNeurons { get; }
    }
}
