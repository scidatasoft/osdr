using MassTransit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ReportGenerated : CorrelatedBy<Guid>
    {
        DateTimeOffset TimeStamp { get; }
        int NumberOfGenericFiles { get; }
    }
}
