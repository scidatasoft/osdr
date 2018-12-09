using System;

namespace Sds.Osdr.Reactions.Sagas.Commands
{
    public interface ProcessReactionFile
    {
        Guid Id { get; set; }
        string Bucket { get; }
        Guid BlobId { get; }
        Guid UserId { get; set; }
    }
}
