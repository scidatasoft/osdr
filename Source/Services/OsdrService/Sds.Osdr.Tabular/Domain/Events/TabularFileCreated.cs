using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.Tabular.Domain.Events
{
    public class TabularFileCreated : IUserEvent
    {
		public TabularFileCreated(Guid id)
		{
			Id = id;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
	}
}
