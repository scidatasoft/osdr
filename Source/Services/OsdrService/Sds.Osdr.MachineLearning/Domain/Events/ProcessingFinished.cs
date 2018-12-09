using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface ProcessingFinished : CorrelatedBy<Guid>
	{
		Guid Id { get; }
		Guid UserId { get; }
		DateTimeOffset TimeStamp { get; }
        string Status { get; }
        string Message { get; }
    }
}
