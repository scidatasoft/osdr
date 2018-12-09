using Sds.Domain;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.ValueObjects
{
    public class Dataset : ValueObject<Dataset>
    {
        public Guid? BlobId { get; private set; }
        public string Bucket { get; private set; }
        public string Description { get; private set; }
        public string Title { get; private set; }

        public Dataset(string title, string description, Guid? blobId = null, string bucket = null)
        {
            BlobId = blobId;
            Bucket = bucket;
            Description = description;
            Title = title;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new List<Object>() { BlobId, Bucket, Description, Title };
        }
    }
}
