using Sds.CqrsLite.Events;
using System;

namespace Sds.Osdr.WebPage.Domain.Events
{
    public class WebPageCreated : IUserEvent
    {
		public WebPageCreated(Guid id, Guid userId, string url)
		{
			Id = id;
            UserId = userId;
            Url = url;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Url { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }

    }
}
