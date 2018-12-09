using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;
using Sds.Osdr.WebApi.Requests;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class MLControllerExtension
    {
        public static async Task<HttpResponseMessage> MachineLearningTrain(this OsdrWebClient client, 
            Guid sourceBlobId, string sourceBucket, Guid userId, Guid parentId, bool optimize)
        {
            var folderId = Guid.NewGuid();
            var createMLModel = new CreateMachineLearningModel
            {
                TargetFolderId = folderId,
                SourceBlobId = sourceBlobId,
                Scaler = "some sting named as Scaler",
                SourceBucket = sourceBucket,
                UserId = userId,
                SourceFileName = "combined lysomotrophic.sdf",
                Methods = new List<string> { "NaiveBayes" },
                TrainingParameter = "Soluble",
                SubSampleSize = 1,
                TestDataSize = new decimal(.1),
                KFold = 2,
                ModelType = "Classification",
                Fingerprints = new List<Fingerprint>
                {
                    new Fingerprint {
                        Type = "ecfp",
                        Radius = 2,
                        Size = 512
                    }
                },
                Optimize = optimize
            };

            var postParameters = JsonConvert.SerializeObject(createMLModel);
            
            var response = await client.PostData("/api/machinelearning/models", postParameters);
            return response;
        }

        public static async Task<HttpResponseMessage> CreateSingleStructurePridiction(this OsdrWebClient client, RunSingleStructurePrediction ssp)
        {
            var postParameters = JsonConvert.SerializeObject(ssp);
            
            var response = await client.PostData("api/machinelearning/predictions/structure", postParameters);
            return response;
        }

        public static async Task<HttpResponseMessage> GetPredictionStatus(this OsdrWebClient client, Guid id)
        {
            return await client.GetData($"api/machinelearning/predictions/{id}/status");
        }

        public static async Task<HttpResponseMessage> MachineLearningCreate(this OsdrWebClient client, Guid parentNodeId, string name)
        {
            return await client.PostData("/api/machinelearning/predictions", $"{{'Name': '{name}', parentId: '{parentNodeId}'}}");
        }
        public static async Task<HttpResponseMessage> MachineLearningPredict(this OsdrWebClient client, 
            Guid parentId, Guid modelBlobId, Guid datasetBlobId, Guid userId, string datasetBucket, Guid modelBucket, string folderName)
        {
            var postData = $@"
			{{
				'TargetFolderId': '{parentId}',
				'DatasetBlobId': '{datasetBlobId}',
				'DatasetBucket': '{datasetBucket}',
				'ModelBlobId': '{modelBlobId}',
				'ModelBucket': '{modelBucket}',
				'UserId': '{userId}'
			}}";
            
            return await client.PostData("/api/machinelearning/predictions", postData);
        }
    }
}