using MassTransit;
using Sds.Imaging.Domain.Models;
using System;

namespace Sds.Imaging.Domain.Commands
{
    public interface GenerateImage : CorrelatedBy<Guid>
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid BlobId { get; }
        string Bucket { get; }
        Image Image { get; }
    }
}
