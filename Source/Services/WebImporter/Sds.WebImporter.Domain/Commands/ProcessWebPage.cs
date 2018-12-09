using MassTransit;
using System;

namespace Sds.WebImporter.Domain.Commands
{
    public interface ProcessWebPage : CorrelatedBy<Guid>
    {
        string Bucket { get; }
        string Url { get; }
        Guid Id { get; }
        int ExpectedVersion { get; }
        Guid UserId { get; }
    }
}
