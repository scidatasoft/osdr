using MassTransit;
using System;

namespace Sds.Osdr.MachineLearning.Domain.Commands
{
    public class CreatePrediction : CorrelatedBy<Guid>
	{
		public readonly Guid FolderId;
		public readonly Guid DatasetBlobId;
		public readonly string DatasetBucket;
		public readonly Guid ModelBlobId;
		public readonly string ModelBucket;

		public CreatePrediction(Guid id,
			Guid correlationId,
			Guid folderId,
			Guid datasetBlobId,
			string datasetBucket,
			Guid modelBlobId,
			string modelBucket,
			Guid userId
		)
		{
			Id = id;
			CorrelationId = correlationId;
			FolderId = folderId;
			DatasetBlobId = datasetBlobId;
			DatasetBucket = datasetBucket;
			ModelBlobId = modelBlobId;
			ModelBucket = modelBucket;
			UserId = userId;
		}

		public Guid Id { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid UserId { get; set; }
		public int ExpectedVersion { get; set; }
	}
}
