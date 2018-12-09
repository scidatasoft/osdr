using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.PdfProcessor.Domain.Events
{
    public class ImageExported : ICorrelatedEvent
    {
        public readonly Guid FileId;
        public readonly int Index;

        public ImageExported(Guid id, Guid correlationId, Guid userId, Guid fileId, int index)
        {
            Id = id;
            FileId = fileId;
            Index = index;
            CorrelationId = correlationId;
            UserId = userId;
            TimeStamp = DateTime.Now;
        }

        public Guid CorrelationId { get; set; }

        public Guid UserId { get; set; }

        public Guid Id { get; set; }

        public int Version { get; set; }

        public DateTimeOffset TimeStamp { get; set; }
    }
}
