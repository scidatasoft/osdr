using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ReportReady : CorrelatedBy<Guid>
    {
    }
}
