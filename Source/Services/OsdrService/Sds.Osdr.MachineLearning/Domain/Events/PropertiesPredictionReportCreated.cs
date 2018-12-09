using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface PropertiesPredictionReportCreated : CorrelatedBy<Guid>
	{
		string Message { get; }
        Guid ReportBlobId { get; }
        string ReportBucket { get; }
		Guid Id { get; }
		Guid UserId { get; }
		DateTimeOffset TimeStamp { get; }
	}
}
