using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Files
{
    public class TotalRecordsUpdated : IUserEvent
    {
        public readonly long TotalRecords;

        public TotalRecordsUpdated(Guid id, Guid userId, long totalRecords)
        {
            Id = id;
            UserId = userId;
            TotalRecords = totalRecords;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
