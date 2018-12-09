using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public class AbortModelTraining : CorrelatedBy<Guid>
	{
		public AbortModelTraining(Guid id, Guid correlationId, Guid userId)
		{
			Id = id;
			CorrelationId = correlationId;
			UserId = userId;
		}

		public Guid Id { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid UserId { get; set; }
		public int ExpectedVersion { get; set; }
	}
}
