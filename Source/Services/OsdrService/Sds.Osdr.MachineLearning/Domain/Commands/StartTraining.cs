using MassTransit;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{

    public class StartTraining : CorrelatedBy<Guid>
	{
		public readonly Guid SourceBlobId;
		public readonly string SourceBucket;
		public readonly Guid ParentId;
		public readonly string SourceFileName;
		public readonly IEnumerable<string> Methods;
		public readonly string ClassName;
        public readonly string Scaler;
        public readonly decimal SubSampleSize;
        public readonly decimal TestDataSize;
        public readonly int KFold;
		public readonly IEnumerable<IDictionary<string, object>> Fingerprints;
        public readonly bool Optimize;
	    public HyperParametersOptimization HyperParameters;
        public int DnnLayers { get; set; }
        public int DnnNeurons { get; set; }

        public StartTraining(Guid id, Guid parentId, Guid sourceBlobId, string sourceBucket, Guid correlationId, Guid userId,
			string sourceFileName,
            IEnumerable<string> methods,
            string scaler,
            string className,
            decimal subSampleSize,
            decimal testDataSize,
			int kFold,
            IEnumerable<IDictionary<string, object>> fingerprints,
            bool optimize,
            HyperParametersOptimization hyperParameters,
            int dnnLayers,
            int dnnNeurons
        )
		{

            Id = id;
			SourceBlobId = sourceBlobId;
			SourceBucket = sourceBucket;
			ParentId = parentId;
			CorrelationId = correlationId;
			UserId = userId;
			SourceFileName = sourceFileName;
			Methods = methods;
			ClassName = className;
			SubSampleSize = subSampleSize;
			TestDataSize = testDataSize;
			KFold = kFold;
            Fingerprints = fingerprints;
            Scaler = scaler;
            Optimize = optimize;
		    HyperParameters = hyperParameters;
            DnnLayers = dnnLayers;
            DnnNeurons = dnnNeurons;

        }

		public Guid Id { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid UserId { get; set; }
		public int ExpectedVersion { get; set; }
	}
}
