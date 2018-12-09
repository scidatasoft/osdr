using Sds.CqrsLite.Events;
using System;

namespace Sds.PdfProcessor.Domain.Events
{
    public class TextExported : ICorrelatedEvent
    {
        public readonly Guid FileId;

        public TextExported(Guid id, Guid correlationId, Guid userId, Guid fileId)
        {
            Id = id;
            FileId = fileId;
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
