using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public class ModelCreationFailed : CorrelatedBy<Guid>
    {
        public string ProgressMessage { get;  }
        public Guid CorrelationId { get; }
        public DateTimeOffset StartModelingTime { get; }
        public string MethodName { get;  }
        public string FileName { get; }

        public ModelCreationFailed(Guid id, Guid userId, Guid correlationId, string progressMessage, string fileName, DateTimeOffset startModelingTime, string methodName = null)
        {
            Id = id;
            UserId = userId;
            CorrelationId = correlationId;
            ProgressMessage = progressMessage;
            StartModelingTime = startModelingTime;
            MethodName = methodName;
            FileName = fileName;
        }

        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public DateTimeOffset TimeStamp { get; set; } = DateTimeOffset.UtcNow;

        public int Version { get; set; }
    }
}
