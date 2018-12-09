using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Events
{
    public interface PropertiesPredicted : CorrelatedBy<Guid>
	{
		string FileBucket { get; }
        Guid FileBlobId { get; }
		Guid Id { get; }
		Guid UserId { get; }
		DateTimeOffset TimeStamp { get; }
	}
}
