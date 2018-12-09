using Automatonymous;
using MassTransit.MongoDbIntegration.Saga;
using Sds.ChemicalFileParser.Domain;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using Sds.Osdr.RecordsFile.Sagas.Events;
using System;

namespace Sds.Osdr.Chemicals.Sagas
{
    public class InvalidSubstanceProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid RecordId { get; set; }
        public Guid FileId { get; set; }
        public long Index { get; set; }
        public string Message { get; set; }
        public Guid UserId { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid FileCorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public int AllPersisted { get; set; }
    }
    public class InvalidSubstanceProcessingStateMachine : MassTransitStateMachine<InvalidSubstanceProcessingState>
    {
        public InvalidSubstanceProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => RecordParseFailed, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => InvalidRecordCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => NodeRecordPersisted, x => x.CorrelateById(context => context.Message.Id));
            Event(() => RecordPersisted, x => x.CorrelateById(context => context.Message.Id));
            
            CompositeEvent(() => EndCreating, x => x.AllPersisted, NodeRecordPersisted, RecordPersisted, InvalidRecordCreated);

            Initially(
                When(RecordParseFailed)
                    .TransitionTo(Creating)
                    .ThenAsync(async context =>
                    {
                        context.Instance.RecordId = context.Data.Id;
                        context.Instance.FileId = context.Data.FileId;
                        context.Instance.Index = context.Data.Index;
                        context.Instance.Message = context.Data.Message;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.Created = context.Data.TimeStamp;
                        context.Instance.FileCorrelationId = context.Data.CorrelationId;

                        await context.Raise(BeginCreating);
                    })
                );

            During(Creating,
                When(BeginCreating)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<CreateInvalidRecord>(new
                        {
                            Id = context.Instance.RecordId,
                            Index = context.Instance.Index,
                            FileId = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            Type = RecordType.Structure,
                            Message = context.Instance.Message,
                            TimeStamp = context.Instance.Updated.UtcDateTime
                        });
                    }),
                When(EndCreating)
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
                        await context.Raise(EndProcessed);
                    }),
                When(EndProcessed)
                    .Then(context =>
                    {
                        context.CreateConsumeContext().Publish<InvalidRecordProcessed>(new
                        {
                            Id = context.Instance.RecordId,
                            Index = context.Instance.Index,
                            Type = RecordType.Structure,
                            FileId = context.Instance.FileId,
                            CorrelationId = context.Instance.FileCorrelationId,
                            UserId = context.Instance.UserId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
                );
        }

        Event<RecordParseFailed> RecordParseFailed { get; set; }
        Event<InvalidRecordCreated> InvalidRecordCreated { get; set; }
        Event<NodeRecordPersisted> NodeRecordPersisted { get; set; }
        Event<RecordPersisted> RecordPersisted { get; set; }

        State Creating { get; set; }
        Event BeginCreating { get; set; }
        Event EndCreating { get; set; }
        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }
    }
}
