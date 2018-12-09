using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Generic.Sagas.Events;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using Sds.Osdr.MachineLearning.Sagas.Events;
using Sds.Storage.Blob.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.MachineLearning.Sagas
{
    public class ModelTrainingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid SourceFileId { get; set; }
        public Guid ModelFolderId { get; set; }
        public string ModelFolderName { get; set; }
        public Guid UserId { get; set; }
        public Guid SourceBlobId { get; set; }
        public string SourceBucket { get; set; }
        public string ProgressMessage { get; set; }
        public string FailureReason { get; set; }
        public string SourceFileName { get; set; }
        public string Method { get; set; }
        public string ClassName { get; set; }
        public decimal SubSampleSize { get; set; }
        public decimal TestDatasetSize { get; set; }
        public int KFold { get; set; }
        public IEnumerable<IDictionary<string, object>> Fingerprints { get; set; }
        public string CurrentState { get; set; }
        public Guid TrainingCorrelationId { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid ModelId { get; set; }
        public Guid _id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public int ModelTrainingStatus { get; set; }
        public ModelStatus Status { get; set; }
        public int NumberOfProcessedGenericFiles { get; set; }
        public int TotalNumberOfGenericFiles { get; set; }
        public ModelInfo ModelInfo { get; set; }
        public bool HasAnyFail { get; set; }
        public bool IsTrainingStarted { get; set; }
        public bool IsNameChanged { get; set; }
        public string Scaler { get; set; }
        public IList<Imaging.Domain.Models.Image> Images { get; set; } = new List<Imaging.Domain.Models.Image>();
        public HyperParametersOptimization HyperParameters { get; set; }
        public int DnnLayers { get; set; }
        public int DnnNeurons { get; set; }
    }

    public class ModelTrainingStateMachine : MassTransitStateMachine<ModelTrainingState>
    {
        public ModelTrainingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => StartModelTraining, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => ModelTrainingStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ModelCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => GenericFileProcessed, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ModelTrained, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TrainingFailed, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ModelPropertiesUpdated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ModelNameUpdated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ModelThumbnailGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ModelBlobLoaded, x => x.CorrelateById(context => context.Message.BlobInfo.Metadata.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase).ContainsKey("correlationId") ? Guid.Parse(context.Message.BlobInfo.Metadata.ToDictionary(c => c.Key, c => c.Value, StringComparer.OrdinalIgnoreCase)["correlationId"].ToString()) : Guid.Empty));
            Event(() => ModelBlobChanged, x => x.CorrelateById(context => context.Message.Id));


            CompositeEvent(() => EndProcessing, x => x.ModelTrainingStatus, ModelTrainingDone, ModelPropertiesUpdateDone, AllGenericFileProcessingDone, ThumbnailsGenerationDone, ModelNameUpdateDone, ModelBlobUpdateDone);

            Initially(
                When(StartModelTraining)
                    .TransitionTo(Creating)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"Model: ModelTrainingStarted");

                        context.Instance.TrainingCorrelationId = context.Data.CorrelationId;
                        context.Instance.ModelId = context.Data.Id;
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.SourceFileId = context.Data.SourceBlobId;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.SourceBlobId = context.Data.SourceBlobId;
                        context.Instance.SourceBucket = context.Data.SourceBucket;
                        context.Instance.Method = context.Data.Method;
                        context.Instance.Scaler = context.Data.Scaler;
                        context.Instance.ClassName = context.Data.ClassName;
                        context.Instance.SubSampleSize = context.Data.SubSampleSize;
                        context.Instance.TestDatasetSize = context.Data.TestDatasetSize;
                        context.Instance.KFold = context.Data.KFold;
                        context.Instance.Fingerprints = context.Data.Fingerprints;
                        context.Instance.ModelFolderId = context.Data.FolderId;
                        context.Instance.NumberOfProcessedGenericFiles = 0;
                        context.Instance.TotalNumberOfGenericFiles = 0;
                        context.Instance.ModelInfo = new ModelInfo();
                        context.Instance.IsTrainingStarted = false;
                        context.Instance.HasAnyFail = false;
                        context.Instance.HyperParameters = context.Data.HyperParameters;
                        context.Instance.DnnLayers = context.Data.DnnLayers;
                        context.Instance.DnnNeurons = context.Data.DnnNeurons;

                        await context.Raise(BeginCreating);
                    })
                );

            During(Creating,
                When(BeginCreating)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<CreateModel>(new
                        {
                            Id = context.Instance.ModelId,
                            ParentId = context.Instance.ModelFolderId,
                            UserId = context.Instance.UserId,
                            Fingerprints = context.Instance.Fingerprints,
                            ClassName = context.Instance.ClassName,
                            SubSampleSize = context.Instance.SubSampleSize,
                            TestDatasetSize = context.Instance.TestDatasetSize,
                            KFold = context.Instance.KFold,
                            Method = context.Instance.Method,
                            Scaler = context.Instance.Scaler
                        });
                    }),

                When(ModelCreated)
                    .TransitionTo(Queuing)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginQueuing);
                    })
                );

            During(Queuing,
                When(BeginQueuing)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Status = ModelStatus.Created;

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.ModelId,
                            Status = context.Instance.Status,
                            UserId = context.Instance.UserId
                        });
                    }),

                When(StatusChanged)
                    .TransitionTo(Processing)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<TrainModel>(new
                        {
                            Id = context.Instance.ModelId,
                            Scaler = context.Instance.Scaler,
                            SourceBlobId = context.Instance.SourceBlobId,
                            SourceBucket = context.Instance.SourceBucket,
                            ParentId = context.Instance.ModelFolderId,
                            Method = context.Instance.Method,
                            ClassName = context.Instance.ClassName,
                            SubSampleSize = context.Instance.SubSampleSize,
                            TestDatasetSize = context.Instance.TestDatasetSize,
                            KFold = context.Instance.KFold,
                            Fingerprints = context.Instance.Fingerprints,
                            UserId = context.Instance.UserId,
                            CorrelationId = context.Instance.CorrelationId,
                            HyperParameters = context.Instance.HyperParameters,
                            DnnLayers = context.Instance.DnnLayers,
                            DnnNeurons = context.Instance.DnnNeurons
                        });
                    }));

            During(Processing,
                When(ModelTrainingStarted)
                    .ThenAsync(async context =>
                    {
                        context.Instance.IsTrainingStarted |= true;
                        context.Instance.Name = context.Data.ModelName;

                        context.Instance.Status = ModelStatus.Training;

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.ModelId,
                            Status = context.Instance.Status,
                            UserId = context.Instance.UserId
                        });

                        await context.CreateConsumeContext().Publish<UpdateModelName>(new
                        {
                            Id = context.Instance.ModelId,
                            ModelName = context.Data.ModelName,
                            Name = context.Instance.Name,
                            UserId = context.Instance.UserId,
                            TimeStamp = context.Data.TimeStamp
                        });
                    }),

                When(ModelNameUpdated)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"Model: ModelNameUpdated");

                        await context.Raise(ModelNameUpdateDone);
                    }),
                When(ModelTrained)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        var message = context.Data;

                        context.Instance.ModelInfo.BlobId = message.BlobId;
                        context.Instance.ModelInfo.Bucket = message.Bucket;

                        context.Instance.TotalNumberOfGenericFiles = context.Data.NumberOfGenericFiles > context.Instance.TotalNumberOfGenericFiles ? context.Data.NumberOfGenericFiles : context.Instance.TotalNumberOfGenericFiles;

                        if (context.Instance.NumberOfProcessedGenericFiles == context.Instance.TotalNumberOfGenericFiles)
                        {
                            Log.Debug($"ML: GenericFileProcessed: Raise AllGenericFileProcessingDone (NumberOfProcessedGenericFiles: {context.Instance.NumberOfProcessedGenericFiles}; TotalNumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles})");
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        Log.Debug($"Model: ModelTrained (NumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles})");

                        context.Instance.Status = ModelStatus.Trained;

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.ModelId,
                            Status = context.Instance.Status,
                            UserId = context.Instance.UserId
                        });

                        await context.CreateConsumeContext().Publish<UpdateModelProperties>(new
                        {
                            Id = message.Id,
                            Name = context.Instance.Name,
                            UserId = message.UserId,
                            TimeStamp = message.TimeStamp,
                            Dataset = new Dataset(message.DatasetTitle, message.DatasetDescription, message.BlobId, message.Bucket),
                            Property = new Property(message.PropertyCategory, message.PropertyName, message.PropertyUnits, message.PropertyDescription),
                            Modi = context.Data.Modi,
                            DisplayModelName = context.Data.DisplayMethodName
                        });
                    }),
                When(ModelBlobLoaded)
                    .ThenAsync(async context => 
                    {
                        await context.CreateConsumeContext().Publish<UpdateModelBlob>(new
                        {
                            Id = context.Instance.ModelId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Data.BlobInfo.Id,
                            Bucket = context.Data.BlobInfo.Bucket,
                            Metadata = context.Data.BlobInfo.Metadata
                        });
                    }),
                When(ModelThumbnailGenerated)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<GenerateImage>(new
                        {
                            Id = context.Instance.ModelId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Data.BlobId,
                            Bucket = context.Data.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            Image = new Imaging.Domain.Models.Image()
                            {
                                Id = NewId.NextGuid(),
                                Width = 300,
                                Height = 300,
                                Format = "PNG",
                                MimeType = "image/png"
                            }
                        });

                        await context.CreateConsumeContext().Publish<GenerateImage>(new
                        {
                            Id = context.Instance.ModelId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Data.BlobId,
                            Bucket = context.Data.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            Image = new Imaging.Domain.Models.Image()
                            {
                                Id = NewId.NextGuid(),
                                Width = 600,
                                Height = 600,
                                Format = "PNG",
                                MimeType = "image/png"
                            }
                        });

                        await context.CreateConsumeContext().Publish<GenerateImage>(new
                        {
                            Id = context.Instance.ModelId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Data.BlobId,
                            Bucket = context.Data.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            Image = new Imaging.Domain.Models.Image()
                            {
                                Id = NewId.NextGuid(),
                                Width = 1200,
                                Height = 1200,
                                Format = "PNG",
                                MimeType = "image/png"
                            }
                        });
                    }),
                When(ImageGenerated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        if (context.Data.Image.Format.ToLower() == "png" && !context.Instance.Images.Contains(context.Data.Image)
                        && (context.Data.Image.Width == 300 || context.Data.Image.Width == 600 || context.Data.Image.Width == 1200))
                        {
                            context.Instance.Images.Add(context.Data.Image);

                            await context.CreateConsumeContext().Publish<AddImage>(new
                            {
                                Id = context.Instance.ModelId,
                                UserId = context.Instance.UserId,
                                Image = new Generic.Domain.ValueObjects.Image(context.Data.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
                            });
                        }
                    }),
                When(ImageAdded)
                    .ThenAsync(async context =>
                    {
                        if (context.Instance.Images.Count == 3)
                        {
                            await context.Raise(ThumbnailsGenerationDone);
                        }
                    }),

                 When(TrainingFailed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.HasAnyFail = true;

                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        if (context.Data.NumberOfGenericFiles == 0)
                        {
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        context.Instance.TotalNumberOfGenericFiles = context.Data.NumberOfGenericFiles > context.Instance.TotalNumberOfGenericFiles ? context.Data.NumberOfGenericFiles : context.Instance.TotalNumberOfGenericFiles;

                        Log.Debug($"Model: ModelTrainingFailed (NumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles})");

                        context.Instance.Status = ModelStatus.Failed;

                        if (context.Instance.NumberOfProcessedGenericFiles == context.Instance.TotalNumberOfGenericFiles)
                        {
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.ModelId,
                            Status = context.Instance.Status,
                            UserId = context.Instance.UserId
                        });

                        if (!context.Instance.IsTrainingStarted)
                        {
                            await context.CreateConsumeContext().Publish<UpdateModelName>(new
                            {
                                Id = context.Instance.ModelId,
                                ModelName = $"Failed-Model-{context.Instance.ModelId}",
                                Name = context.Instance.Name,
                                UserId = context.Instance.UserId,
                                TimeStamp = context.Data.TimeStamp
                            });
                        }

                        if (!context.Data.IsThumbnailGenerated)
                        {
                            await context.Raise(ThumbnailsGenerationDone);
                        }

                        if (!context.Data.IsModelTrained)
                        {
                            Log.Debug($"Model: ModelTrainingFailed - Raise ModelTrainingDone");
                            await context.Raise(ModelPropertiesUpdateDone);
                            await context.Raise(ModelBlobUpdateDone);
                        }
                    }),

                 When(StatusChanged)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.Status == ModelStatus.Trained || context.Data.Status == ModelStatus.Failed)
                        {
                            await context.Raise(ModelTrainingDone);
                        }
                    }),

                When(ModelPropertiesUpdated)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"Model: ModelBlobUpdated");

                        await context.Raise(ModelPropertiesUpdateDone);
                    }),

                When(ModelBlobChanged)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"Model: ModelBlobUpdated");

                        await context.Raise(ModelBlobUpdateDone);
                    }),

                When(GenericFileProcessed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.NumberOfProcessedGenericFiles++;

                        context.Instance.ModelInfo.GenericFiles.Add(context.Data.BlobId);

                        if (context.Instance.NumberOfProcessedGenericFiles == context.Instance.TotalNumberOfGenericFiles)
                        {
                            Log.Debug($"ML: GenericFileProcessed: Raise AllGenericFileProcessingDone (NumberOfProcessedGenericFiles: {context.Instance.NumberOfProcessedGenericFiles}; TotalNumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles})");
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        Log.Debug($"ML: GenericFileProcessed (TotalNumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles}; NumberOfProcessedGenericFiles: {context.Instance.NumberOfProcessedGenericFiles})");
                    }),

                When(EndProcessing)
                    .TransitionTo(Processed)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"Model: EndProcessing");

                        await context.Raise(BeginProcessed);
                    })
                );

            During(Processed,
                When(BeginProcessed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Status = context.Instance.HasAnyFail ? ModelStatus.Failed : ModelStatus.Processed;

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.ModelId,
                            Status = context.Instance.Status,
                            UserId = context.Instance.UserId
                        });
                    }),
                When(StatusChanged)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(EndProcessed);
                    }),
                When(EndProcessed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<ModelTrainingFinished>(new
                        {
                            Id = context.Instance.ModelId,
                            CorrelationId = context.Instance.TrainingCorrelationId,
                            Timestamp = DateTimeOffset.UtcNow,
                            Status = context.Instance.Status,
                            ModelInfo = context.Instance.ModelInfo,
                            ParentId = context.Instance.ModelFolderId,
                            UserId = context.Instance.UserId
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        State Creating { get; set; }
        Event BeginCreating { get; set; }
        Event EndCreating { get; set; }

        State Queuing { get; set; }
        Event BeginQueuing { get; set; }
        Event EndQueuing { get; set; }

        State Processing { get; set; }
        Event BeginProcessing { get; set; }
        Event EndProcessing { get; set; }

        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }

        Event<StartModelTraining> StartModelTraining { get; set; }
        Event<ModelPropertiesUpdated> ModelPropertiesUpdated { get; set; }
        Event<ModelNameUpdated> ModelNameUpdated { get; set; }
        Event<ModelTrainingStarted> ModelTrainingStarted { get; set; }
        Event<ModelBlobChanged> ModelBlobChanged { get; set; }
        Event<ModelCreated> ModelCreated { get; set; }
        Event<ModelTrained> ModelTrained { get; set; }
        Event<BlobLoaded> ModelBlobLoaded { get; set; }
        Event<FileProcessed> GenericFileProcessed { get; set; }
        Event<TrainingFailed> TrainingFailed { get; set; }
        Event<StatusChanged> StatusChanged { get; set; }
        Event<ModelThumbnailGenerated> ModelThumbnailGenerated { get; set; }
        Event<ImageGenerated> ImageGenerated { get; set; }
        Event<ImageAdded> ImageAdded { get; set; }

        Event ModelTrainingDone { get; set; }
        Event ModelPropertiesUpdateDone { get; set; }
        Event ModelBlobUpdateDone { get; set; }
        Event ModelNameUpdateDone { get; set; }
        Event ThumbnailsGenerationDone { get; set; }
        Event AllGenericFileProcessingDone { get; set; }
    }
}