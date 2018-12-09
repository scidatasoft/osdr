using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Pdf.Domain.Events
{
    public class PdfFileCreated : IUserEvent
    {
		public PdfFileCreated(Guid id)
		{
			Id = id;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
	}
}
