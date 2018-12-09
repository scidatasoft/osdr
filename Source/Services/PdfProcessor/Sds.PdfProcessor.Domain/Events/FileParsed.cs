using Sds.CqrsLite.Events;
using Sds.PdfProcessor.Domain.Commands;
using System;
using System.Collections.Generic;

namespace Sds.PdfProcessor.Domain.Events
{
    public class FileParsed : ICorrelatedEvent
    {
        public int ByteTypes { get; set; }

        public FileParsed(Guid id, Guid correlationId, Guid userId, int byteTypes)
        {
            Id = id;
            ByteTypes = byteTypes;
            CorrelationId = correlationId;
            UserId = userId;
            TimeStamp = DateTime.Now;
        }

        public Guid Id { get; set; }

        public DateTimeOffset TimeStamp { get; set; }

        public int Version { get; set; }

        public Guid CorrelationId { get; set; }

        public Guid UserId { get; set; }
    }
}
