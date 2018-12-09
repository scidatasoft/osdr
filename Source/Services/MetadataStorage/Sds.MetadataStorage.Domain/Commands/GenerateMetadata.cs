using System;

namespace Sds.MetadataStorage.Domain.Commands
{
    public interface GenerateMetadata
    {
        Guid FileId { get; }
    }
}
