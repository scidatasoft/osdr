using MassTransit;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface TrainModel : CorrelatedBy<Guid>
	{
        Guid SourceBlobId { get; }
        string SourceBucket { get; }
        Guid ParentId { get; } 
        string Method { get; }
        string ClassName { get; }
        decimal SubSampleSize { get; }
        decimal TestDatasetSize { get; }
        string Scaler { get; }
        int KFold { get; }
        IEnumerable<IDictionary<string, object>> Fingerprints { get; }
		Guid Id { get; set; }
		Guid UserId { get; set; }
	    HyperParametersOptimization HyperParameters { get; }
        int DnnLayers { get; }
        int DnnNeurons { get; }
    }
}
