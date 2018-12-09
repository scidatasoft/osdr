using MassTransit;
using Sds.CqrsLite.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelCreated : CorrelatedBy<Guid>, IUserEvent
    {
        public readonly IDictionary<string, object> ModelInfo;
        public Guid ParentId { get; set; }
        public ModelStatus Status { get; set; }
        public int KFold { get; set; }
        public decimal TestDatasetSize { get; set; }
        public decimal SubSampleSize { get; set; }
        public string ClassName { get; set; }
        public string Scaler { get; set; }
        public string Method { get; set; }
        public IEnumerable<Fingerprint> Fingerprints { get; set; }
        public Guid? BlobId { get; set; }
        public string Bucket { get; set; }
        public Dataset Dataset { get; set; }
        public Property Property { get; set; }
        public string Name { get; set; }
        public string DisplayMethodName { get; set; }
        public IDictionary<string, object> Metadata { get; set; }

        public ModelCreated(
            Guid id,
            Guid userId,
            Guid parentId,
            ModelStatus status,
            Dataset dataset,
            Property property,
            string method,
            string scaler,
            int kFold,
            decimal testDatasetSize,
            decimal subSampleSize,
            string className,
            IEnumerable<Fingerprint> fingerprints,
            Guid? blobId,
            string bucket,
            string name,
            string displayMethodName,
            IDictionary<string, object> metadata)
        {
            Id = id;
            UserId = userId;
            ParentId = parentId;
            Status = status;
            KFold = kFold;
            TestDatasetSize = testDatasetSize;
            SubSampleSize = subSampleSize;
            ClassName = className;
            Method = method;
            Fingerprints = fingerprints;
            Scaler = scaler;
            BlobId = blobId;
            Bucket = bucket;
            Dataset = dataset;
            Property = property;
            Name = name;
            DisplayMethodName = displayMethodName;
            Metadata = metadata;
        }

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public int Version { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
