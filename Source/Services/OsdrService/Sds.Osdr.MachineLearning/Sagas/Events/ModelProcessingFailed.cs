using Sds.CqrsLite.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Sagas.Events
{
    public class ModelProcessingFailed : IUserEvent
    {
        public string ProgressMessage;
        public Guid CorrelationId;

        public ModelProcessingFailed(Guid id, Guid userId, Guid correlationId, string progressMessage)
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
