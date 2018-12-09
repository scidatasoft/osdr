using Sds.CqrsLite.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelPropertiesUpdated : IUserEvent
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;
        public Guid UserId { get; set; }
        public long Length { get; set; }
        public Property Property { get; set; }
        public Dataset Dataset { get; set; }
        public double Modi { get; set; }
        public string DisplayMethodName { get; set; }

        public ModelPropertiesUpdated(Guid id, Guid userId, Dataset dataset, Property property, double modi, string displayMethodName)
        {
            Id = id;
            UserId = userId;
            Property = property;
            Modi = modi;
            Dataset = dataset;
            DisplayMethodName = displayMethodName;
        }
    }
}
