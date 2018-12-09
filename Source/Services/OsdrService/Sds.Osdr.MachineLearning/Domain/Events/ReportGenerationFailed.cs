using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ReportGenerationFailed : CorrelatedBy<Guid>
    {
        DateTimeOffset TimeStamp { get; }
        string Message { get; }
        int NumberOfGenericFiles { get; }
        Guid UserId { get; }
    }
}
