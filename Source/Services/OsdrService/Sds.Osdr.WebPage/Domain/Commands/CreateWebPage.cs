using Sds.Osdr.Generic.Domain;
using System;

namespace Sds.Osdr.WebPage.Domain.Commands
{
    public interface CreateWebPage
    {
        Guid Id { get; }
        Guid UserId { get; }
        Guid ParentId { get; }
        string Name { get; }
        Guid FileId { get; }
        FileStatus Status { get; }
        string Bucket { get; }
        Guid BlobId { get; }
        long Lenght { get; }
        string Md5 { get; }  
        string Url { get; }
    }
}
