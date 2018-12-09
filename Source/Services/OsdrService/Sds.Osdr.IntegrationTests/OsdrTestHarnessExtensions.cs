using MassTransit;
using Sds.MassTransit.Extensions;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Commands.Users;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Storage.Blob.Domain.Dto;
using Sds.Storage.Blob.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests
{
    public static class OsdrTestHarnessExtensions
    {
        public static async Task<Guid> AddBlob(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException(path);

            var blobId = await harness.BlobStorage.AddFileAsync(fileName, System.IO.File.OpenRead(path), "application/octet-stream", bucket, metadata);

            return blobId;
        }

        public static async Task<Guid> CreateUser(this OsdrTestHarness harness, string displayName, string firstName, string lastName, string loginName, string email, string avatar, Guid userId)
        {
            Guid id = NewId.NextGuid();

            await harness.CreateUser(id, displayName, firstName, lastName, loginName, email, avatar, userId);

            return id;
        }

        public static async Task CreateUser(this OsdrTestHarness harness, Guid id, string displayName, string firstName, string lastName, string loginName, string email, string avatar, Guid userId)
        {
            await harness.BusControl.Publish<CreateUser>(new
            {
                Id = id,
                DisplayName = displayName,
                FirstName = firstName,
                LastName = lastName,
                LoginName = loginName,
                Email = email,
                Avatar = avatar,
                UserId = userId
            });

            if (!harness.Received.Select<Generic.Domain.Events.Users.UserPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.UserPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static async Task<Guid> CreateFolder(this OsdrTestHarness harness, string name, Guid parentId, Guid userId)
        {
            Guid id = NewId.NextGuid();

            await harness.BusControl.Publish<CreateFolder>(new
            {
                Id = id,
                Name = name,
                ParentId = parentId,
                UserId = userId
            });

            if (!harness.Received.Select<Generic.Domain.Events.Folders.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            return id;
        }

        public static async Task<Guid> ProcessFile(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            Log.Information($"Processing started [{fileName}]");

            var blobId = await harness.AddBlob(bucket, fileName, metadata);

            var blobInfo = await harness.BlobStorage.GetFileInfo(blobId, bucket);

            await harness.BusControl.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, harness.JohnId, blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            if (!harness.Received.Select<Generic.Sagas.Events.FileProcessed>(m => m.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }
            var fileProcessed = harness.Received.Select<Generic.Sagas.Events.FileProcessed>(m => m.Context.Message.BlobId == blobId).First();
            var fileId = fileProcessed.Context.Message.Id;

            if (!harness.Received.Select<Generic.Domain.Events.Files.NodeStatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }
            if (!harness.Received.Select<Generic.Domain.Events.Files.StatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }

            var events = await harness.CqrsEventStore.Get(fileId, 0);

            Log.Information($"Aggregate-{fileId}: {string.Join(", ", events.Select(e => $"{e.GetType().Name} ({e.Version})"))}");

            Log.Information($"Processing finished [{fileName}]");

            return fileId;
        }

        public static async Task<Guid> ProcessRecordsFile(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            Log.Information($"Processing started [{fileName}]");

            var blobId = await harness.AddBlob(bucket, fileName, metadata.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase));

            var blobInfo = await harness.BlobStorage.GetFileInfo(blobId, bucket);

            await harness.BusControl.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, (Guid)metadata["parentId"], blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            if (!harness.Received.Select<RecordsFile.Sagas.Events.RecordsFileProcessed>(m => m.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }
            var fileProcessed = harness.Received.Select<RecordsFile.Sagas.Events.RecordsFileProcessed>(m => m.Context.Message.BlobId == blobId).First();
            var fileId = fileProcessed.Context.Message.Id;

            if (fileProcessed.Context.Message.ProcessedRecords != harness.GetProcessedRecords(fileId).Count())
            {
                throw new Exception($"fileProcessed.Context.Message.ProcessedRecords ({fileProcessed.Context.Message.ProcessedRecords}) != Fixture.GetProcessedRecords(fileId).Count() ({harness.GetProcessedRecords(fileId).Count()})");
            }

            foreach (var recordId in harness.GetProcessedRecords(fileId))
            {
                if (!harness.Received.Select<RecordsFile.Domain.Events.Records.NodeStatusPersisted>(m => m.Context.Message.Status == RecordsFile.Domain.RecordStatus.Processed && m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
                if (!harness.Received.Select<RecordsFile.Domain.Events.Records.StatusPersisted>(m => m.Context.Message.Status == RecordsFile.Domain.RecordStatus.Processed && m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
            }

            if (fileProcessed.Context.Message.FailedRecords != harness.GetInvalidRecords(fileId).Count())
            {
                throw new Exception("fileProcessed.Context.Message.FailedRecords != Fixture.GetInvalidRecords(fileId).Count()");
            }

            foreach (var recordId in harness.GetInvalidRecords(fileId))
            {
                if (!harness.Received.Select<RecordsFile.Domain.Events.Records.RecordPersisted>(m => m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
                if (!harness.Received.Select<RecordsFile.Domain.Events.Records.NodeRecordPersisted>(m => m.Context.Message.Id == recordId).Any())
                {
                    throw new TimeoutException();
                }
            }

            if (!harness.Received.Select<Generic.Domain.Events.Files.NodeStatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }
            if (!harness.Received.Select<Generic.Domain.Events.Files.StatusPersisted>(m => (m.Context.Message.Status == Generic.Domain.FileStatus.Processed || m.Context.Message.Status == Generic.Domain.FileStatus.Failed) && m.Context.Message.Id == fileId).Any())
            {
                throw new TimeoutException();
            }

            var events = await harness.CqrsEventStore.Get(fileId, 0);

            Log.Information($"Aggregate-{fileId}: {string.Join(", ", events.Select(e => $"{e.GetType().Name} ({e.Version})"))}");

            Log.Information($"Processing finished [{fileName}]");

            return fileId;
        }

        public static async Task<Guid> TrainModel(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await harness.AddBlob(harness.JohnId.ToString(), fileName, metadata);

            Guid correlationId = NewId.NextGuid();
            var modelFolderId = NewId.NextGuid();
            if (metadata["case"].Equals("valid one model with success optimization")
               || metadata["case"].Equals("train model with failed optimization"))
            {
                await harness.BusControl.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = harness.JohnId,
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
                await harness.BusControl.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = harness.JohnId,
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
                    Optimize = false
                });
            }

            if (metadata["case"].Equals("two valid models"))
            {
                await harness.BusControl.Publish<StartTraining>(new
                {
                    Id = modelFolderId,
                    ParentId = modelFolderId,
                    SourceBlobId = blobId,
                    SourceBucket = bucket,
                    CorrelationId = correlationId,
                    UserId = harness.JohnId,
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

            if (!harness.Received.Select<MachineLearning.Domain.Events.TrainingFinished>(m => m.Context.Message.Id == modelFolderId).Any())
            {
                throw new TimeoutException();
            }

            return modelFolderId;
        }

        public static async Task<Guid> PredictProperties(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await harness.AddBlob(harness.JohnId.ToString(), fileName, metadata);

            Guid predictionFolderId = Guid.NewGuid();
            Guid correlationId = Guid.NewGuid();
            Guid modelBlobId = Guid.NewGuid();

            await harness.BusControl.Publish(new CreatePrediction(
                id: predictionFolderId,
                correlationId: correlationId,
                folderId: predictionFolderId,
                datasetBlobId: blobId,
                datasetBucket: bucket,
                modelBlobId: modelBlobId,
                modelBucket: harness.JohnId.ToString(),
                userId: harness.JohnId
            ));

            if (!harness.Received.Select<MachineLearning.Sagas.Events.PropertiesPredictionFinished>(m => m.Context.Message.Id == predictionFolderId).Any())
            {
                throw new TimeoutException();
            }

            return predictionFolderId;
        }

        public static async Task<Guid> UploadModel(this OsdrTestHarness harness, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var blobId = await harness.AddBlob(bucket, fileName, metadata);

            var blobInfo = await harness.BlobStorage.GetFileInfo(blobId, bucket);

            await harness.BusControl.Publish<BlobLoaded>(new
            {
                BlobInfo = new LoadedBlobInfo(blobId, fileName, blobInfo.Length, harness.JohnId, blobInfo.UploadDateTime, blobInfo.MD5, bucket, blobInfo.Metadata),
                TimeStamp = DateTimeOffset.UtcNow
            });

            if (!harness.Received.Select<MachineLearning.Domain.Events.ModelPersisted>(e => e.Context.Message.BlobId == blobId).Any())
            {
                throw new TimeoutException();
            }

            var modelProcessed = harness.Received.Select<MachineLearning.Domain.Events.ModelPersisted>(m => m.Context.Message.BlobId == blobId).First();
            return modelProcessed.Context.Message.Id;
        }

        public static void WaitWhileModelShared(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<MachineLearning.Domain.Events.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            //if (!harness.Received.Select<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            //{
            //    throw new TimeoutException();
            //}
        }

        public static void WaitWhileFileShared(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<Generic.Domain.Events.Files.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        //public static void WaitWhileFolderShared(this BusTestHarness harness, Guid id)
        //{
        //    if (!harness.Published.Select<Generic.Domain.Events.Folders.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
        //    {
        //        throw new TimeoutException();
        //    }

        //    if (!harness.Published.Select<Generic.Domain.Events.Nodes.PermissionChangedPersisted>(m => m.Context.Message.Id == id).Any())
        //    {
        //        throw new TimeoutException();
        //    }
        //}

        public static void WaitWhileFolderCreated(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<Generic.Domain.Events.Folders.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.FolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        //public static void WaitWhileModelTrained(this BusTestHarness harness, Guid folderId)
        //{
        //    if (!harness.Published.Select<TrainingFinished>(m => m.Context.Message.Id == folderId).Any())
        //    {
        //        throw new TimeoutException();
        //    }
        //}

        public static void WaitWhileFolderRenamed(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<Generic.Domain.Events.Folders.RenamedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.RenamedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static void WaitWhileFolderMoved(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<Generic.Domain.Events.Folders.MovedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.MovedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        public static void WaitWhileFolderDeleted(this OsdrTestHarness harness, Guid id)
        {
            if (!harness.Received.Select<Generic.Domain.Events.Folders.DeletedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }

            if (!harness.Received.Select<Generic.Domain.Events.Nodes.DeletedFolderPersisted>(m => m.Context.Message.Id == id).Any())
            {
                throw new TimeoutException();
            }
        }

        //public static async Task<bool> DeleteFolder(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteFolder>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}

        //public static async Task<bool> DeleteFile(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteFile>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}

        //public static async Task<bool> DeleteRecord(this BusTestHarness harness, Guid id, Guid userId, int expectedVersion = 0)
        //{
        //    await harness.Bus.Publish<DeleteRecord>(new
        //    {
        //        Id = id,
        //        UserId = userId,
        //        ExpectedVersion = expectedVersion
        //    });

        //    return await harness.WaitWhileAllProcessed();
        //}
    }
}
