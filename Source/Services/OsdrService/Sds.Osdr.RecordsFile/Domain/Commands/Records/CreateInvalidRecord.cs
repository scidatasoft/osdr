using System;
using Sds.CqrsLite.Events;

namespace Sds.Osdr.RecordsFile.Domain.Commands
{
    public interface CreateInvalidRecord : IUserEvent
    {
        //Guid Id { get; set; }
        Guid FileId { get; }
        long Index { get; }
        RecordType Type { get; }
        string Message { get; }
        //Guid UserId { get; set; }
        //DateTimeOffset TimeStamp { get; set; }
        //int Version { get; set; }
    }
}
