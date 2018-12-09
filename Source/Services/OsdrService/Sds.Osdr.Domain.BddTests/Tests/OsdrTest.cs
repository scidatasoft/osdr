using CQRSlite.Domain;
using CQRSlite.Events;
using MassTransit;
using MassTransit.Testing;
using MongoDB.Driver;
using Sds.Osdr.Domain.BddTests.Extensions;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Sagas.Events;
using Sds.Storage.Blob.Core;
using Sds.Storage.Blob.Domain.Dto;
using Sds.Storage.Blob.Events;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests
{
    [CollectionDefinition("OSDR Test Harness")]
    public class OsdrTestCollection : ICollectionFixture<OsdrTestHarness>
    {
    }

    public abstract class OsdrTest : IClassFixture<ContainerTest>
    {
        protected Guid JohnId => Fixture.JohnId;
        protected Guid JaneId => Fixture.JaneId;
        protected OsdrTestHarness Fixture { get; }
        protected IBus Bus => Fixture.BusControl;
        protected ISession Session => Fixture.Session;
        protected IBlobStorage BlobStorage => Fixture.BlobStorage;
        protected IEventStore CqrsEventStore => Fixture.CqrsEventStore;
        protected EventStore.IEventStore EventStore => Fixture.EventStore;
        protected BusTestHarness Harness => Fixture.Harness;
        protected ContainerTest Container { get; }

        protected IMongoCollection<dynamic> Nodes => Fixture.MongoDb.GetCollection<dynamic>("Nodes");
        protected IMongoCollection<dynamic> Files => Fixture.MongoDb.GetCollection<dynamic>("Files");
        protected IMongoCollection<dynamic> Folders => Fixture.MongoDb.GetCollection<dynamic>("Folders");
        protected IMongoCollection<dynamic> Users => Fixture.MongoDb.GetCollection<dynamic>("Users");
        protected IMongoCollection<dynamic> Models => Fixture.MongoDb.GetCollection<dynamic>("Models");
        protected IMongoCollection<dynamic> Records => Fixture.MongoDb.GetCollection<dynamic>("Records");

        public OsdrTest(OsdrTestHarness fixture, ITestOutputHelper output = null, ContainerTest container = null)
        {
            Fixture = fixture;
            Container = container;

            if (output != null)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo
                    .TestOutput(output, LogEventLevel.Verbose)
                    .CreateLogger()
                    .ForContext<OsdrTest>();
            }
        }

        protected async Task<Guid> AddBlob(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException(path);

            var blobId = await BlobStorage.AddFileAsync(fileName, System.IO.File.OpenRead(path), "application/octet-stream", bucket, metadata);

            return blobId;
        }

        protected Guid GetBlobId(Guid fileId)
        {
            var fileCreated = Harness.Consumed.Select<Generic.Domain.Events.Files.FileCreated>(m => m.Context.Message.Id == fileId).First();
            return fileCreated.Context.Message.BlobId;
        }

        protected async Task<Guid> ProcessRecordsFile(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            Log.Information($"Processing started [{fileName}]");

            var blobId = await AddBlob(bucket, fileName, metadata.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase));

            var blobInfo = await BlobStorage.GetFileInfo(blobId, bucket);

            await Bus.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, Guid.Parse(bucket), blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });
            Harness.Published.Select();

            if (!Harness.Published.Select<RecordsFile.Sagas.Events.RecordsFileProcessed>(m => m.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }
            var fileProcessed = Harness.Published.Select<RecordsFile.Sagas.Events.RecordsFileProcessed>(m => m.Context.Message.BlobId == blobId).First();
            var fileId = fileProcessed.Context.Message.Id;

            if (fileProcessed.Context.Message.ProcessedRecords != Fixture.GetProcessedRecords(fileId).Count())
            {
                throw new Exception($"fileProcessed.Context.Message.ProcessedRecords ({fileProcessed.Context.Message.ProcessedRecords}) != Fixture.GetProcessedRecords(fileId).Count() ({Fixture.GetProcessedRecords(fileId).Count()})");
            }

            foreach (var recordId in Fixture.GetProcessedRecords(fileId))
            {
                if (!Harness.Published.Select<RecordsFile.Domain.Events.Records.NodeStatusPersisted>(m => m.Context.Message.Status == RecordsFile.Domain.RecordStatus.Processed && m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
                if (!Harness.Published.Select<RecordsFile.Domain.Events.Records.StatusPersisted>(m => m.Context.Message.Status == RecordsFile.Domain.RecordStatus.Processed && m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
            }

            if (fileProcessed.Context.Message.FailedRecords != Fixture.GetInvalidRecords(fileId).Count())
            {
                throw new Exception("fileProcessed.Context.Message.FailedRecords != Fixture.GetInvalidRecords(fileId).Count()");
            }

            foreach (var recordId in Fixture.GetInvalidRecords(fileId))
            {
                if (!Harness.Published.Select<RecordsFile.Domain.Events.Records.RecordPersisted>(m => m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
                if (!Harness.Published.Select<RecordsFile.Domain.Events.Records.NodeRecordPersisted>(m => m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
            }

            if (!Harness.Published.Select<Generic.Domain.Events.Files.NodeStatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }
            if(!Harness.Published.Select<Generic.Domain.Events.Files.StatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }

            var events = await CqrsEventStore.Get(fileId, 0);

            Log.Information($"Aggregate-{fileId}: {string.Join(", ", events.Select(e => $"{e.GetType().Name} ({e.Version})"))}");

            Log.Information($"Processing finished [{fileName}]");

            return fileId;
        }

        public async Task<Guid> ProcessFile(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            Log.Information($"Processing started [{fileName}]");

            var blobId = await AddBlob(bucket, fileName, metadata);

            var blobInfo = await BlobStorage.GetFileInfo(blobId, bucket);

            await Bus.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, JohnId, blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            if (!Harness.Published.Select<Generic.Sagas.Events.FileProcessed>(m => m.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }
            var fileProcessed = Harness.Published.Select<Generic.Sagas.Events.FileProcessed>(m => m.Context.Message.BlobId == blobId).First();
            var fileId = fileProcessed.Context.Message.Id;

            if (!Harness.Published.Select<Generic.Domain.Events.Files.NodeStatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }
            if (!Harness.Published.Select<Generic.Domain.Events.Files.StatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }

            var events = await CqrsEventStore.Get(fileId, 0);

            Log.Information($"Aggregate-{fileId}: {string.Join(", ", events.Select(e => $"{e.GetType().Name} ({e.Version})"))}");

            Log.Information($"Processing finished [{fileName}]");

            return fileId;
        }

        protected async Task<Guid> TrainModel(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await AddBlob(JohnId.ToString(), fileName, metadata);
            
            Guid correlationId = NewId.NextGuid();
            var modelFolderId = NewId.NextGuid();
            if(metadata["case"].Equals("valid one model with success optimization") 
               || metadata["case"].Equals("train model with failed optimization"))
            {
                await Harness.Bus.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = JohnId,
                    SourceFileName = fileName,
                    Methods = new List<string>(new[]
                    {
                        "NaiveBayes"
                    }),
                    ClassName = "ClassName",
                    Optimize = true
                });
            }
            else if (!metadata["case"].Equals("two valid models"))
            {
                await Harness.Bus.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = JohnId,
                    SourceFileName = fileName,
                    Scaler = "Somebody knows what is Scaler???",
                    Methods = new List<string>(new[]
                    {
                        "NaiveBayes"
                    }),
                    ClassName = "ClassName",
                    SubSampleSize = (decimal)0.2,
                    TestDataSize = (decimal)0.2,
                    KFold = 4,
                    Fingerprints = new List<IDictionary<string, object>>()
                    {
                        new Dictionary<string, object>()
                        {
                            { "radius", 3 },
                            { "size", 1024 },
                            { "type", FingerprintType.ecfp }
                        }
                    },
                    Optimize = false,
                    HyperParameters = new HyperParametersOptimization() { NumberOfIterations = 100, OptimizationMethod = "OptimizationMethod" }
                });
            }

            if (metadata["case"].Equals("two valid models"))
            {
                await Harness.Bus.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = JohnId,
                    SourceFileName = fileName,
                    Scaler = "Somebody knows what is Scaler???",
                    Methods = new List<string>(new[]
                    {
                        "NaiveBayes",
                        "LogisticRegression"
                    }),
                    ClassName = "ClassName",
                    SubSampleSize = (decimal)0.2,
                    TestDataSize = (decimal)0.2,
                    KFold = 4,
                    Fingerprints = new List<IDictionary<string, object>>()
                    {
                        new Dictionary<string, object>()
                        {
                            { "radius", 3 },
                            { "size", 1024 },
                            { "type", FingerprintType.ecfp }
                        }
                    },
                    Optimize = false
                });
            }



            if (!Harness.Published.Select<TrainingFinished>(m => m.Context.Message.Id == modelFolderId).Any())
            {
                throw new TimeoutException();
            }
            
            return modelFolderId;
        }

        protected async Task<Guid> PredictProperties(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await AddBlob(JohnId.ToString(), fileName, metadata);

            Guid predictionFolderId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();
            Guid modelBlobId = Guid.NewGuid();

            await Harness.Bus.Publish(new CreatePrediction(
                id: predictionFolderId,
                correlationId: correlationId,
                folderId: predictionFolderId,
                datasetBlobId: blobId,
                datasetBucket: bucket,
                modelBlobId: modelBlobId,
                modelBucket: JohnId.ToString(),
                userId: JohnId
            ));

            if (!Harness.Published.Select<PropertiesPredictionFinished>(m => m.Context.Message.Id == predictionFolderId).Any())
            {
                throw new TimeoutException();
            }

            return predictionFolderId;
        }

        protected async Task<Guid> UploadModel(string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await AddBlob(bucket, fileName, metadata);

            var blobInfo = await BlobStorage.GetFileInfo(blobId, bucket);

            await Bus.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, JohnId, blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            if (!Harness.Published.Select<ModelPersisted>(e => e.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }

            return blobId;
        }


        //protected async Task<Guid> LoadWebPage(Guid userId, string bucket, string url)
        //{
        //    var pageId = Guid.NewGuid();
        //    var correlationId = Guid.NewGuid();

        //    await Bus.Publish<UploadWebPage>(new
        //    {
        //        Id = pageId,
        //        Bucket = bucket,
        //        Url = new Uri(url),
        //        UserId = userId,
        //        ParentId = userId,
        //        CorrelationId = correlationId
        //    });

        //    return pageId;
        //}
    }
}