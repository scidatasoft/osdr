using System;
using System.Collections.Generic;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public interface UpdateModelBlob
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        IDictionary<string, object> Metadata { get; } 
    }
}
