using Sds.CqrsLite.Events;
using System;

namespace Sds.PdfProcessor.Domain.Events
{
    public class MetaExtractionFailed : ICorrelatedEvent
    {
        public readonly string Message;

        public MetaExtractionFailed(Guid id, Guid correlationId, Guid userId, string message)
        {
            Id = id;
            Message = message;
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
