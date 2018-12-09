using Sds.CqrsLite.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using System;

namespace Sds.Osdr.RecordsFile.Domain.Events.Records
{
    public class ImageAdded : IUserEvent
    {
        public Image Image { get; private set; }

        public ImageAdded(Guid id, Guid userId, Image image)
        {
            Id = id;
            UserId = userId;
            Image = image;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
