using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface PropertiesPredictionFailed : CorrelatedBy<Guid>
	{
		string Message { get; }
		Guid Id { get; }
		Guid UserId { get; }
		DateTimeOffset TimeStamp { get; }
	}
}
