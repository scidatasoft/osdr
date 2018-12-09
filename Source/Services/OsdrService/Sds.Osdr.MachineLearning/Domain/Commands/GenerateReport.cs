using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public class ModelInfo
    {
        public string Bucket { get; set; }
        public Guid? BlobId { get; set; }
        public IList<Guid> GenericFiles { get; set; }

        public ModelInfo()
        {
            GenericFiles = new List<Guid>();
        }
    }

    public interface GenerateReport : CorrelatedBy<Guid>
    {
        Guid ParentId { get; }
        string SourceBucket { get; set; }
        Guid SourceBlobId { get; set; }
        DateTimeOffset TimeStamp { get; }
        IEnumerable<ModelInfo> Models { get; }
        Guid UserId { get; }
    }
}
