using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Osdr.Generic.Domain.Events.Folders;
using Sds.Osdr.Generic.Sagas.Events;
using Sds.Osdr.MachineLearning.Domain;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Domain.ValueObjects;
using Sds.Osdr.MachineLearning.Sagas.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.MachineLearning.Sagas
{
    public class TrainingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid ParentId { get; set; }
        public Guid UserId { get; set; }
        public Guid SourceBlobId { get; set; }
        public string SourceBucket { get; set; }
        public int NumberOfProcessedModels { get; set; }
        public int ExpectedNumberOfProcessedModels { get; set; }
        public string SourceFileName { get; set; }
        public IEnumerable<string> Methods { get; set; }
        public string ClassName { get; set; }
        public decimal SubSampleSize { get; set; }
        public decimal TestDatasetSize { get; set; }
        public int KFold { get; set; }
        public IList<ModelInfo> ModelInfos { get; set; }
        public IEnumerable<IDictionary<string, object>> Fingerprints { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public int ModelTrainingStatus { get; set; }
        public int ModelOptimizationStatus { get; set; }
        public int DocumentsStatus { get; set; }
        public int NumberOfProcessedGenericFiles { get; set; }
        public int TotalNumberOfGenericFiles { get; set; }
        public string Message { get; set; }
        public string Scaler { get; set; }
        public int AllPersisted { get; set; }
        public TrainingStatus Status { get; set; }
        public bool Optimize { get; set; }
        public HyperParametersOptimization HyperParameters { get; set; }
        public int DnnLayers { get; set; }
        public int DnnNeurons { get; set; }
    }

    public class TrainingStateMachine : MassTransitStateMachine<TrainingState>
    {
        public TrainingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => StartTraining, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => ModelTrainingAborted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FolderDeleted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ModelTrainingFinished, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TrainingFinished, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ModelTrainingFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => GenericFileProcessed, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => ReportGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ReportGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TrainingOptimized, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TrainingOptimizationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));


            CompositeEvent(() => EndOptimization, x => x.ModelOptimizationStatus, OptimizationDone, OptimizationMetricsProcessingDone);
            CompositeEvent(() => EndTraining, x => x.ModelTrainingStatus, ReportGenerationDone, AllGenericFileProcessingDone);

            DuringAny(
                When(FolderDeleted)
                    .Finalize());

            Initially(
                When(StartTraining)
                    .TransitionTo(Optimization)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.NumberOfProcessedModels = 0;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.ParentId = context.Data.ParentId;
                        context.Instance.SourceBlobId = context.Data.SourceBlobId;
                        context.Instance.SourceBucket = context.Data.SourceBucket;
                        context.Instance.SourceFileName = context.Data.SourceFileName;
                        context.Instance.Methods = context.Data.Methods;
                        context.Instance.ClassName = context.Data.ClassName;
                        context.Instance.SubSampleSize = context.Data.SubSampleSize;
                        context.Instance.TestDatasetSize = context.Data.TestDataSize;
                        context.Instance.KFold = context.Data.KFold;
                        context.Instance.Fingerprints = context.Data.Fingerprints;
                        context.Instance.NumberOfProcessedGenericFiles = 0;
                        context.Instance.TotalNumberOfGenericFiles = 0;
                        context.Instance.ExpectedNumberOfProcessedModels = context.Instance.Methods.ToList().Count;
                        context.Instance.Message = "";
                        context.Instance.Scaler = context.Data.Scaler;
                        context.Instance.Status = TrainingStatus.Started;
                        context.Instance.ModelInfos = new List<ModelInfo>();
                        context.Instance.Optimize = context.Data.Optimize;
                        context.Instance.HyperParameters = context.Data.HyperParameters;
                        context.Instance.DnnLayers = context.Data.DnnLayers;
                        context.Instance.DnnNeurons = context.Data.DnnNeurons;

                        await context.Raise(BeginOptimization);
                    }));

            During(Optimization,
                When(BeginOptimization)
                    .ThenAsync(async context =>
                    {
                        if (context.Instance.Optimize)
                        {
                            await context.CreateConsumeContext().Publish<OptimizeTraining>(new
                            {
                                Id = NewId.NextGuid(),
                                UserId = context.Instance.UserId,
                                SourceFileName = context.Instance.SourceFileName,
                                TargetFolderId = context.Instance.ParentId,
                                TimeStamp = DateTimeOffset.UtcNow,
                                SourceBlobId = context.Instance.SourceBlobId,
                                SourceBucket = context.Instance.SourceBucket,
                                Methods = context.Instance.Methods,
                                ClassName = context.Instance.ClassName,
                                CorrelationId = context.Instance.CorrelationId,
                                DnnLayers = context.Instance.DnnLayers,
                                DnnNeurons = context.Instance.DnnNeurons
                            });
                        }
                        else
                        {
                            await context.Raise(EndOptimization);
                        }
                    }),

                When(TrainingOptimized)
                    .ThenAsync(async context =>
                    {
                        context.Instance.SubSampleSize = context.Data.SubSampleSize;
                        context.Instance.TestDatasetSize = context.Data.TestDataSize;
                        context.Instance.KFold = context.Data.KFold;
                        context.Instance.Fingerprints = context.Data.Fingerprints;
                        context.Instance.Scaler = context.Data.Scaler;
                        context.Instance.HyperParameters = context.Data.HyperParameters;

                        await context.Raise(OptimizationDone);
                    }),
                When(GenericFileProcessed)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(OptimizationMetricsProcessingDone);
                    }),

                When(TrainingOptimizationFailed)
                    .Then(context =>
                    {
                        context.Instance.Status = TrainingStatus.Failed;
                        context.Instance.Message = context.Data.Message;
                    })
                    .TransitionTo(Processed)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginProcessed);
                    }),

                When(EndOptimization)
                    .TransitionTo(Training)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginTraining);
                    }));


            During(Training,
                When(BeginTraining)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Status = TrainingStatus.Training;

                        foreach (var method in context.Instance.Methods)
                        {
                            await context.CreateConsumeContext().Publish<StartModelTraining>(new
                            {
                                SourceBlobId = context.Instance.SourceBlobId,
                                SourceBucket = context.Instance.SourceBucket,
                                Scaler = context.Instance.Scaler,
                                FolderId = context.Instance.ParentId,
                                Method = method,
                                ClassName = context.Instance.ClassName,
                                SubSampleSize = context.Instance.SubSampleSize,
                                TestDatasetSize = context.Instance.TestDatasetSize,
                                KFold = context.Instance.KFold,
                                Fingerprints = context.Instance.Fingerprints,
                                Id = NewId.NextGuid(),
                                UserId = context.Instance.UserId,
                                CorrelationId = context.Instance.CorrelationId,
                                HyperParameters = context.Instance.HyperParameters,
                                DnnLayers = context.Instance.DnnLayers,
                                DnnNeurons = context.Instance.DnnNeurons
                            });
                        }
                    }),

                When(ModelTrainingFinished)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        if (context.Data.Status == ModelStatus.Processed)
                        {
                            context.Instance.ModelInfos.Add(context.Data.ModelInfo);
                        }

                        context.Instance.NumberOfProcessedModels++;

                        if (context.Instance.NumberOfProcessedModels == context.Instance.ExpectedNumberOfProcessedModels)
                        {
                            if (context.Instance.ModelInfos.Count != 0)
                            {
                                await context.CreateConsumeContext().Publish<GenerateReport>(new
                                {
                                    SourceBucket = context.Instance.SourceBucket,
                                    SourceBlobId = context.Instance.SourceBlobId,
                                    ParentId = context.Instance.ParentId,
                                    TimeStamp = DateTimeOffset.UtcNow,
                                    UserId = context.Instance.UserId,
                                    Models = context.Instance.ModelInfos,
                                    CorrelationId = context.Instance.CorrelationId
                                });
                                Log.Debug($"ML: ModelProcessingFinished: Raise AllModelsProcessingDone (NumberOfProcessedModels: {context.Instance.NumberOfProcessedModels}; ExpectedNumberOfProcessedModels: {context.Instance.ExpectedNumberOfProcessedModels})");
                            }
                            else
                            {
                                await context.Raise(AllGenericFileProcessingDone);

                                await context.Raise(ReportGenerationDone);
                            }
                        }
                    }),

                When(ReportGenerated)
                    .ThenAsync(async context =>
                    {
                        context.Instance.TotalNumberOfGenericFiles = context.Data.NumberOfGenericFiles;

                        if (context.Instance.TotalNumberOfGenericFiles == 0)
                        {
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        await context.Raise(ReportGenerationDone);
                    }),

                When(ReportGenerationFailed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.TotalNumberOfGenericFiles = context.Data.NumberOfGenericFiles;

                        if (context.Instance.TotalNumberOfGenericFiles == 0)
                        {
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        await context.Raise(ReportGenerationDone);
                    }),

                When(GenericFileProcessed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.NumberOfProcessedGenericFiles++;

                        if (context.Instance.NumberOfProcessedGenericFiles == context.Instance.TotalNumberOfGenericFiles)
                        {
                            Log.Debug($"ML: GenericFileProcessed: Raise AllGenericFileProcessingDone (NumberOfProcessedGenericFiles: {context.Instance.NumberOfProcessedGenericFiles}; TotalNumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles})");
                            await context.Raise(AllGenericFileProcessingDone);
                        }

                        Log.Debug($"ML: GenericFileProcessed (TotalNumberOfGenericFiles: {context.Instance.TotalNumberOfGenericFiles}; NumberOfProcessedGenericFiles: {context.Instance.NumberOfProcessedGenericFiles})");
                    }),

                When(EndTraining)
                    .TransitionTo(Processed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Status = TrainingStatus.Fimished;
                        context.Instance.Message = "Models training finished.";
                        await context.Raise(BeginProcessed);
                    }));

            During(Processed,
                When(BeginProcessed)
                    .ThenAsync(async context =>
                    {
                        Log.Debug("ML: BeginProcessed");

                        await context.CreateConsumeContext().Publish<TrainingFinished>(new
                        {
                            Id = context.Instance.ParentId,
                            UserId = context.Instance.UserId,
                            CorrelationId = context.Instance.CorrelationId,
                            NumberOfGenericFiles = context.Instance.NumberOfProcessedGenericFiles,
                            Timestamp = DateTimeOffset.UtcNow,
                            Message = context.Instance.Message,
                            Status = context.Instance.Status
                        });
                    })
                    .Finalize()
                );
            SetCompletedWhenFinalized();
        }

        State Optimization { get; set; }
        Event BeginOptimization { get; set; }
        Event EndOptimization { get; set; }

        State Training { get; set; }
        Event BeginTraining { get; set; }
        Event EndTraining { get; set; }

        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }

        Event<StartTraining> StartTraining { get; set; }
        Event<TrainingOptimized> TrainingOptimized { get; set; }
        Event<TrainingOptimizationFailed> TrainingOptimizationFailed { get; set; }
        Event<ModelTrainingAborted> ModelTrainingAborted { get; set; }
        Event<FolderDeleted> FolderDeleted { get; set; }
        Event<ModelTrainingFinished> ModelTrainingFinished { get; set; }
        Event<TrainingFinished> TrainingFinished { get; set; }
        Event<TrainingFailed> ModelTrainingFailed { get; set; }
        Event<FileProcessed> GenericFileProcessed { get; set; }

        Event<ReportGenerated> ReportGenerated { get; set; }
        Event<ReportGenerationFailed> ReportGenerationFailed { get; set; }

        Event ReportGenerationDone { get; set; }
        Event OptimizationDone { get; set; }
        Event OptimizationMetricsProcessingDone { get; set; }
        Event AllGenericFileProcessingDone { get; set; }
        Event AllPersisted { get; set; }
        Event NodeStatusPersistenceDone { get; set; }
        Event StatusPersistenceDone { get; set; }
    }
}
