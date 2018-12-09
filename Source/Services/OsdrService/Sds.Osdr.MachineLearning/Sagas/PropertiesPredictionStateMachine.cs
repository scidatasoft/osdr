using Automatonymous;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Folders;
using Sds.Osdr.Generic.Domain.Events.Folders;
using Sds.Osdr.Generic.Sagas.Events;
using Sds.Osdr.MachineLearning.Domain.Commands;
using Sds.Osdr.MachineLearning.Domain.Events;
using Sds.Osdr.MachineLearning.Sagas.Events;
using System;

namespace Sds.Osdr.MachineLearning.Sagas
{
    public class PropertiesPredictionState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid FolderId { get; set; }
        public Guid UserId { get; set; }
        public Guid DatasetBlobId { get; set; }
        public string DatasetBucket { get; set; }
        public Guid ModelBlobId { get; set; }
        public string ModelBucket { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public string Message { get; set; }
        public int PredictionStatus { get; set; }
    }

    public class PropertiesPredictionStateMachine : MassTransitStateMachine<PropertiesPredictionState>
    {
        public PropertiesPredictionStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => CreatePrediction, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => PropertiesPredicted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => GenericFileProcessed, x => x.CorrelateById(context => context.Message.ParentId));
            Event(() => PropertiesPredictionFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PropertiesPredictionReportCreated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => PropertiesPredictionFinished, x => x.CorrelateById(context => context.Message.CorrelationId));

            CompositeEvent(() => EndProcessing, x => x.PredictionStatus, PredictionDone, PredictionReportProcessingDone);

            Initially(
                When(CreatePrediction)
                .TransitionTo(Processing)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.FolderId = context.Data.FolderId;
                        context.Instance.DatasetBlobId = context.Data.DatasetBlobId;
                        context.Instance.DatasetBucket = context.Data.DatasetBucket;
                        context.Instance.ModelBlobId = context.Data.ModelBlobId;
                        context.Instance.ModelBucket = context.Data.ModelBucket;
                        context.Instance.UserId = context.Data.UserId;
   
                        await context.Raise(BeginProcessing);
                    }));

            During(Processing,
                When(BeginProcessing)
                    .ThenAsync(async context =>
                    {

                        await context.CreateConsumeContext().Publish<PredictProperties>(new
                        {
                            Id = context.Instance.CorrelationId,
                            CorrelationId = context.Instance.CorrelationId,
                            ParentId = context.Instance.FolderId,
                            DatasetBlobId = context.Instance.DatasetBlobId,
                            DatasetBucket = context.Instance.DatasetBucket,
                            ModelBlobId = context.Instance.ModelBlobId,
                            ModelBucket = context.Instance.ModelBucket,
                            UserId = context.Instance.UserId
                        });
                    }),

                When(PropertiesPredicted)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Message = "Prediction successfuly finished.";

                        await context.Raise(PredictionDone);
                    }),

                When(PropertiesPredictionFailed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Message = $"Error: {context.Data.Message}";

                        await context.Raise(EndProcessing);
                    }),

                When(GenericFileProcessed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(PredictionReportProcessingDone);
                    }),

                 When(EndProcessing)
                 .TransitionTo(Processed)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginProcessed);
                    })
                );

            During(Processed,
                When(BeginProcessed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<PropertiesPredictionFinished>(new
                        {
                            Id = context.Instance.FolderId,
                            UserId = context.Instance.UserId,
                            CorrelationId = context.Instance.CorrelationId,
                            Timestamp = DateTimeOffset.UtcNow,
                            Message = context.Instance.Message
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }
        
        public State Processing { get; private set; }
        public Event BeginProcessing { get; set; }
        public Event EndProcessing { get; set; }
        public Event PredictionDone { get; set; }
        public Event PredictionReportProcessingDone { get; set; }

        public State Processed { get; private set; }
        public Event BeginProcessed { get; set; }

        public Event<CreatePrediction> CreatePrediction { get; set; }
        public Event<PropertiesPredicted> PropertiesPredicted { get; private set; }
        public Event<PropertiesPredictionFailed> PropertiesPredictionFailed { get; private set; }
        public Event<FileProcessed> GenericFileProcessed { get; private set; }
        public Event<PropertiesPredictionReportCreated> PropertiesPredictionReportCreated { get; private set; }
        public Event<PropertiesPredictionFinished> PropertiesPredictionFinished { get; private set; }
    }
}
