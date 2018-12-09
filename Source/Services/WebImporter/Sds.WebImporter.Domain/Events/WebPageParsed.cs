using MassTransit;
using System;
using System.Collections.Generic;

namespace Sds.WebImporter.Domain.Events
{
    public interface WebPageParsed : CorrelatedBy<Guid>
    {
        long TotalRecords { get; }
        Guid FileId { get; }
        IEnumerable<string> Fields { get; }
        Guid Id { get; }
        Guid UserId { get; }
        DateTimeOffset TimeStamp { get; }
    }
}
