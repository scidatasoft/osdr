using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Generic.Domain.Events.Folders
{
    public class ProcessingProgressChanged : IUserEvent
    {
		public readonly string ProgressMessage;
		public readonly Guid CorrelationId;

		public ProcessingProgressChanged(Guid id, Guid userId, Guid correlationId, string progressMessage)
        {
            Id = id;
            UserId = userId;
			CorrelationId = correlationId;
			ProgressMessage = progressMessage;
		}

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
