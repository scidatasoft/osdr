using MassTransit;
using Newtonsoft.Json;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class MachineLearning : OsdrService,
        IConsumer<PredictProperties>,
        IConsumer<TrainModel>,
        IConsumer<GenerateReport>,
        IConsumer<OptimizeTraining>,
        IConsumer<PredictStructure>

    {
        public MachineLearning(IBlobStorage blobStorage)
            : base(blobStorage)
        {
        }

        public async Task Consume(ConsumeContext<PredictProperties> context)
        {
            switch (context.Message.DatasetBucket)
            {
                case "failed_case":
                    {
                        await context.Publish<PropertiesPredictionFailed>(new
                        {

                            Message = "It`s business, nothing personal.",
                            Id = context.Message.ParentId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow,
                            CorrelationId = context.Message.CorrelationId

                        });

                        break;
                    }
                default:
                    var bucket = context.Message.DatasetBucket;

                    var predictionsBlobId = await LoadBlob(context, context.Message.UserId, bucket, "PropertiesPrediction.csv",
                        "text/csv", new Dictionary<string, object>() { { "parentId", context.Message.ParentId } });

                    await context.Publish<PropertiesPredicted>(new
                    {
                        Id = predictionsBlobId,
                        FileBucket = context.Message.DatasetBucket,
                        FileBlobId = predictionsBlobId,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });

                    break;
            }
        }

        public async Task Consume(ConsumeContext<TrainModel> context)
        {
            var message = context.Message;
            var bucket = message.SourceBucket;
            var correlationId = message.CorrelationId;
            var modelSagaCorrelationId = Guid.NewGuid();
            var folderId = message.ParentId;
            var userId = message.UserId;

            var blobInfo = await BlobStorage.GetFileInfo(message.SourceBlobId, bucket);

            if (blobInfo.FileName.ToLower().Equals("combined lysomotrophic.sdf") && ((blobInfo.Metadata["case"].Equals("valid one model") || (blobInfo.Metadata["case"].Equals("two valid models")) || blobInfo.Metadata["case"].Equals("valid one model with success optimization"))))
            {
                //  Success path
                //  1. Training just one model and generate one image and one CSV
                //  2. Generate one image and one CSV assigned to the training process
                //  3. Generate report PDF at the end

                var modelId = message.Id;

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var modelBlobId = await LoadBlob(context, userId, bucket,
                    "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", "application/octet-stream",
                    new Dictionary<string, object>()
                    {
                        {"parentId", message.ParentId},
                        {"correlationId", correlationId},
                        {"FileType", "MachineLearningModel"},
                        {
                            "ModelInfo", new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                        },
                        {"SkipOsdrProcessing", true}
                    });

                await context.Publish<ModelTrained>(new
                {
                    Id = modelId,
                    BlobId = modelBlobId,
                    Bucket = message.SourceBucket,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    NumberOfGenericFiles = 5,
                    CorrelationId = correlationId,
                    PropertyName = "name",
                    PropertyCategory = "category",
                    PropertyUnits = "units",
                    PropertyDescription = "description",
                    DatasetTitle = "dataset",
                    DatasetDescription = "really, this is dataset",
                    Modi = 0.2,
                    DisplayMethodName = "Naive Bayes",
                    Metadata = new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                });

                var thummbnailBlobId = await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelThumbnailGenerated>(new
                {
                    Id = modelId,
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Bucket = bucket,
                    BlobId = thummbnailBlobId
                });


                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });
            }
            else if (blobInfo.FileName.ToLower().Equals("combined lysomotrophic.sdf") && blobInfo.Metadata["case"].Equals("valid one model (with delays)"))
            {
                //  Success path
                //  1. Training just one model and generate one image and one CSV
                //  2. Generate one image and one CSV assigned to the training process
                //  3. Generate report PDF at the end

                var modelId = message.Id;

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });

                Thread.Sleep(10);

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var thummbnailBlobId = await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelThumbnailGenerated>(new
                {
                    Id = modelId,
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Bucket = bucket,
                    BlobId = thummbnailBlobId
                });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var modelBlobId = await LoadBlob(context, userId, bucket,
                    "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", "application/octet-stream",
                    new Dictionary<string, object>()
                    {
                        {"parentId", message.ParentId},
                        { "correlationId", correlationId },
                        { "FileType", "MachineLearningModel"},
                        {
                            "ModelInfo", new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                        },
                        {"SkipOsdrProcessing", true}
                    });

                Thread.Sleep(10);

                await context.Publish<ModelTrained>(new
                {
                    Id = modelId,
                    BlobId = modelBlobId,
                    Bucket = message.SourceBucket,
                    NumberOfGenericFiles = 2,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = correlationId,
                    Property = new Property("category", "name", "units", "description"),
                    Dataset = new Dataset("dataset", "really, this is dataset", message.SourceBlobId, message.SourceBucket),
                    Modi = 0.2,
                    DisplayMethodName = "Naive Bayes",
                    Metadata = new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "ML_report.pdf",
                    "application/pdf", new Dictionary<string, object>() { { "parentId", modelId } });

                Thread.Sleep(10);
            }
            else if (blobInfo.FileName.ToLower().Equals("combined lysomotrophic.sdf") && blobInfo.Metadata["case"].Equals("valid one model (reverse events order)"))
            {
                //  Success path (reverse events order)
                //  1. Training just one model and generate one image and one CSV
                //  2. Generate one image and one CSV assigned to the training process
                //  3. Generate report PDF at the end

                var modelId = message.Id;

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var modelBlobId = await LoadBlob(context, userId, bucket,
                    "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", "application/octet-stream",
                    new Dictionary<string, object>()
                    {
                        {"parentId", message.ParentId},
                        {"correlationid", correlationId },
                        {"FileType", "MachineLearningModel"},
                        {
                            "ModelInfo", new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                        },
                        {"SkipOsdrProcessing", true}
                    });

                await context.Publish<ModelTrained>(new
                {
                    Id = modelId,
                    BlobId = modelBlobId,
                    Bucket = message.SourceBucket,
                    NumberOfGenericFiles = 5,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = correlationId,
                    Property = new Property("category", "name", "units", "description"),
                    Dataset = new Dataset("dataset", "really, this is dataset", message.SourceBlobId, message.SourceBucket),
                    Modi = 0.2,
                    DisplayMethodName = "Naive Bayes",
                    Metadata = new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                });

                var thummbnailBlobId = await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelThumbnailGenerated>(new
                {
                    Id = modelId,
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Bucket = bucket,
                    BlobId = thummbnailBlobId
                });

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });
            }
            else if (blobInfo.FileName.ToLower().Equals("drugbank_10_records.sdf") && blobInfo.Metadata["case"].Equals("train one model and fail during the training"))
            {
                //  Failed path
                //  1. Training just one model, generate one image and one CSV
                //  2. Issue ModelTrainingFailed during model training

                var modelId = message.Id;

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<TrainingFailed>(new
                {
                    Id = modelId,
                    NumberOfGenericFiles = 2,
                    IsModelTrained = false,
                    IsThumbnailGenerated = false,
                    Message = "Something very bad has happened right after the starting...",
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    UserId = userId
                });
            }
            else if (blobInfo.FileName.ToLower().Equals("drugbank_10_records.sdf") && blobInfo.Metadata["case"].Equals("fail before starting training"))
            {
                //  Failed path
                //  1. Issue ModelTrainingFailed right before starting the first model training

                var modelId = message.Id;

                await context.Publish<TrainingFailed>(new
                {
                    Id = modelId,
                    NumberOfGenericFiles = 0,
                    IsModelTrained = false,
                    IsThumbnailGenerated = false,
                    Message = "Something very bad has happened right after the starting...",
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    UserId = userId
                });
            }
            else if (blobInfo.FileName.ToLower().Equals("drugbank_10_records.sdf") && blobInfo.Metadata["case"].Equals("train one model and fail during the report generation"))
            {
                //  Failed path
                //  1. Training just one model, generate one image and one CSV
                //  2. Successfully finish model's training
                //  3. Generate one image and one CSV assigned to the training
                //  4. Issue ModelTrainingFailed during the report generation

                var modelId = message.Id;

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var modelBlobId = await LoadBlob(context, userId, bucket,
                    "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", "application/octet-stream",
                    new Dictionary<string, object>()
                    {
                        {"parentId", message.ParentId},
                        {"correlationid", correlationId },
                        {"FileType", "MachineLearningModel"},
                        {
                            "ModelInfo", new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                        },
                        {"SkipOsdrProcessing", true}
                    });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var thummbnailBlobId = await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelThumbnailGenerated>(new
                {
                    Id = modelId,
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Bucket = bucket,
                    BlobId = thummbnailBlobId
                });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelTrained>(new
                {
                    Id = modelId,
                    BlobId = modelBlobId,
                    Bucket = message.SourceBucket,
                    NumberOfGenericFiles = 5,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = correlationId,
                    Property = new Property("category", "name", "units", "description"),
                    Dataset = new Dataset("dataset", "really, this is dataset", message.SourceBlobId, message.SourceBucket),
                    Modi = 0.2,
                    DisplayMethodName = "Naive Bayes",
                    Metadata = new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                });
            }
            else if (blobInfo.FileName.ToLower().Equals("combined lysomotrophic.sdf") &&
                     blobInfo.Metadata["case"].Equals("valid one model (reverse events order)"))
            {
                //  Success path
                //  3. Training just one model and generate one image and one CSV
                //  2. Generate one image and one CSV assigned to the training process
                //  1. Generate report PDF at the end

                var modelId = message.Id;

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                     "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "ML_report.pdf",
                    "application/pdf", new Dictionary<string, object>() { { "parentId", modelId } });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var thummbnailBlobId = await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                await context.Publish<ModelThumbnailGenerated>(new
                {
                    Id = folderId,
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Bucket = bucket,
                    BlobId = thummbnailBlobId
                });

                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", modelId } });

                var modelBlobId = await LoadBlob(context, userId, bucket,
                    "Bernoulli_Naive_Bayes_with_isotonic_class_weights.sav", "application/octet-stream",
                    new Dictionary<string, object>()
                    {
                          {"parentId", message.ParentId},
                          {"correlationid", correlationId },
                          {"FileType", "MachineLearningModel"},
                          {
                              "ModelInfo", new Dictionary<string, object>()
                              {
                                  {"ModelName", "Naive Bayes"},
                                  {"SourceBlobId", message.SourceBlobId},
                                  {"Method", message.Method},
                                  {"SourceBucket", userId.ToString()},
                                  {"ClassName", message.ClassName},
                                  {"SubSampleSize", message.SubSampleSize},
                                  {"KFold", message.KFold},
                                  {"Fingerprints", message.Fingerprints}
                              }
                          },
                          {"SkipOsdrProcessing", true}
                    });

                await context.Publish<ModelTrained>(new
                {
                    Id = modelId,
                    ModelBlobId = modelBlobId,
                    ModelBucket = message.SourceBucket,
                    NumberOfGenericFiles = 2,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    Property = new Property("category", "name", "units", "description"),
                    Dataset = new Dataset("dataset", "really, this is dataset", message.SourceBlobId, message.SourceBucket),
                    Modi = 0.2,
                    DisplayMethodName = "Naive Bayes",
                    Metadata = new Dictionary<string, object>()
                            {
                                {"ModelName", "Naive Bayes"},
                                {"SourceBlobId", message.SourceBlobId},
                                {"Method", message.Method},
                                {"SourceBucket", userId.ToString()},
                                {"ClassName", message.ClassName},
                                {"SubSampleSize", message.SubSampleSize},
                                {"KFold", message.KFold},
                                {"Fingerprints", message.Fingerprints}
                            }
                });

                await context.Publish<ModelTrainingStarted>(new
                {
                    Id = modelId,
                    UserId = userId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    ModelName = "Naive Bayes",
                    CorrelationId = correlationId
                });
            }
        }

        public async Task Consume(ConsumeContext<GenerateReport> context)
        {
            var message = context.Message;
            var correlationId = message.CorrelationId;
            var folderId = message.ParentId;
            var bucket = message.Models.First().Bucket;
            var userId = message.UserId;
            var modelBlobId = message.Models.First().BlobId;

            var blobInfo = await BlobStorage.GetFileInfo(message.SourceBlobId, message.SourceBucket);
            if (blobInfo.Metadata["case"].Equals("train one model and fail during the report generation"))
            {
                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                "application/octet-stream", new Dictionary<string, object>() { { "parentId", folderId } });

                await LoadBlob(context, userId, bucket, "ML_report.pdf",
                    "application/pdf", new Dictionary<string, object>() { { "parentId", folderId } });

                await context.Publish<ReportGenerationFailed>(new
                {
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = correlationId,
                    Message = "Something wrong happened during report generation...",
                    NumberOfGenericFiles = 2
                });
            }
            else
            {
                await LoadBlob(context, userId, bucket, "FocusSynthesis_InStock.csv",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", folderId } });

                await LoadBlob(context, userId, bucket, "ML_report.pdf",
                    "application/pdf", new Dictionary<string, object>() { { "parentId", folderId } });

                await LoadBlob(context, userId, bucket, "ml-training-image.png",
                    "application/octet-stream", new Dictionary<string, object>() { { "parentId", folderId } });

                await context.Publish<ReportGenerated>(new
                {
                    CorrelationId = correlationId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    NumberOfGenericFiles = 3
                });
            }
        }

        public async Task Consume(ConsumeContext<OptimizeTraining> context)
        {
            var message = context.Message;
            var correlationId = message.CorrelationId;
            var bucket = message.SourceBucket;
            var userId = message.UserId;

            var blobInfo = await BlobStorage.GetFileInfo(message.SourceBlobId, message.SourceBucket);

            if (blobInfo.Metadata["case"].Equals("valid one model with success optimization"))
            {
                await context.Publish<TrainingOptimized>(new
                {
                    Id = message.Id,
                    CorrelationId = correlationId,
                    UserId = message.UserId,
                    Scaler = "Somebody knows what is Scaler???",
                    SubSampleSize = (decimal)1,
                    TestDataSize = (decimal)0.2,
                    KFold = 4,
                    Fingerprints = new List<IDictionary<string, object>>()
                        {
                            new Dictionary<string, object>()
                            {
                                { "radius", 2 },
                                { "size", 512 },
                                { "type", "ecfp" }
                            }
                        }
                });
            }

            if (blobInfo.Metadata["case"].Equals("train model with failed optimization"))
            {
                await context.Publish<TrainingOptimizationFailed>(new
                {
                    Id = message.Id,
                    CorrelationId = correlationId,
                    UserId = message.UserId,
                    Message = "It`s just test. Nothing personal."
                });
            }
        }

        public async Task Consume(ConsumeContext<PredictStructure> context)
        {
            var begin = DateTime.UtcNow;
            var resultHeader = $@"{{'predictionElapsedTime': 0 }}";


            var modelJson = $@"{{
                    'id': 'cca570be-e95e-47c7-85aa-41b0f055ce93',
                    'predictionElapsedTime': 0,
                    'trainingParameters': {{
                      'method': 'Naive Bayes',
                      'fingerprints': [
                        {{
                          'type': 'DESC'
                        }},
                        {{
                          'type': 'FCFP',
                          'radius': 3,
                          'size': 512
                        }}
                      ],
                      'name': 'model`s name',
                      'scaler': 'Still don`t know what is scaler',
                      'kFold': 2,
                      'testDatasetSize': 0.1,
                      'subSampleSize': 1,
                      'className': 'Soluble',
                      'consensusWeight': 125.61,
                      'modi': 0.2
                    }},
                    'applicabilityDomain': {{
                      'distance': 'distance',
                      'density': 'density'
                    }},
                    'property': {{
                      'code': 'some property code',
                      'category': 'toxicity',
                      'name': 'LC50',
                      'units': 'mg/L',
                      'description': 'LC50 description'
                    }},
                    'dataset': {{
                      'title': 'Dataset title',
                      'description': 'Dataset description'
                    }},
                    'result': {{
                      'value': 3.28,
                      'error': 1.341
                    }}
            }}";

            dynamic response = new ExpandoObject();
            var models = new List<dynamic>();
            var valueObj = JsonConvert.DeserializeObject<dynamic>(resultHeader);
            response.predictionElapsedTime = 0;

            for (int i = 0; i < context.Message.Models.ToList().Count(); i++)
            {
                var modelObj = JsonConvert.DeserializeObject<dynamic>(modelJson);
                var modelId = new Guid(context.Message.Models.ElementAt(i)["Id"].ToString());
                modelObj["id"] = modelId;
                modelObj["predictionElapsedTime"] = DateTime.UtcNow.Subtract(begin).TotalMilliseconds;
                models.Add(modelObj);
            }
            response.predictionElapsedTime = DateTime.UtcNow.Subtract(begin).TotalMilliseconds;

            response.models = models;

            await context.Publish<PredictedResultReady>(new
            {
                Id = context.Message.Id,
                Data = response
            });
        }
    }
}