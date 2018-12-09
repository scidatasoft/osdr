using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.WebApi.Requests
{
    public class CreateMachineLearningModel
    {
        public Guid UserId { get; set; }
        public string TrainingParameter { get; set; }
        public decimal TestDataSize { get; set; }
        public Guid TargetFolderId { get; set; }
        public decimal SubSampleSize { get; set; }
        public string SourceFileName { get; set; }
        public string SourceBucket { get; set; }
        public Guid SourceBlobId { get; set; }
        public string Scaler { get; set; }
        public string ModelType { get; set; }
        public int KFold { get; set; }
        public IEnumerable<string> Methods { get; set; }
        public IEnumerable<Fingerprint> Fingerprints { get; set; }
        public bool Optimize { get; set; }
        public HyperParametersOptimization HyperParameters { get; set; }
        public int DnnLayers { get; set; }
        public int DnnNeurons { get; set; }
    }
}