using MassTransit;
using Sds.Storage.Blob.Domain.Dto;
using System;

namespace Sds.Storage.Blob.Events
{
    public interface BlobLoaded
    {
        LoadedBlobInfo BlobInfo { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
