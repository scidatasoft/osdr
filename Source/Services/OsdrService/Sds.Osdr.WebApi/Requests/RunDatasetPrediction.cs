using Sds.Osdr.MachineLearning.Domain;
using System;

namespace Sds.Osdr.WebApi.Requests
{
    public class RunDatasetPrediction
	{
		public Guid TargetFolderId { get; set; }
		public Guid DatasetBlobId { get; set; }
		public string DatasetBucket { get; set; }
		public Guid ModelBlobId { get; set; }
		public string ModelBucket { get; set; }
		public Guid UserId { get; set; }
	}
}
