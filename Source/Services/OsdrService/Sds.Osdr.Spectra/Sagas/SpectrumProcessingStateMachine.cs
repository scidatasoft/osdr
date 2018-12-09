using Automatonymous;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Domain;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using Sds.Osdr.Spectra.Domain.Commands;
using Sds.Osdr.Spectra.Domain.Events;
using Sds.Osdr.Spectra.Sagas.Events;
using Sds.SpectraFileParser.Domain.Events;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Spectra.Sagas
{
    public class SpectrumProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid RecordId { get; set; }
        public Guid FileId { get; set; }
        public long Index { get; set; }
        public Guid UserId { get; set; }
        public Guid BlobId { get; set; }
        public string Bucket { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid FileCorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public IEnumerable<Field> Fields { get; set; }
		public int AllPersisted { get; set; }
    }

    public class SpectrumProcessingStateMachine : MassTransitStateMachine<SpectrumProcessingState>
    {
        public SpectrumProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => RecordParsed, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => SpectrumCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => SpectrumProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
	        Event(() => NodeStatusPersisted, x => x.CorrelateById(context => context.Message.Id));
	        Event(() => StatusPersisted, x => x.CorrelateById(context => context.Message.Id));

            CompositeEvent(() => AllPersisted, x => x.AllPersisted, StatusChanged, StatusPersistenceDone, NodeStatusPersistenceDone);
            
            Initially(
                When(RecordParsed)
                    .TransitionTo(Creating)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.RecordId = context.Data.Id;
                        context.Instance.FileId = context.Data.FileId;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.BlobId = context.Data.BlobId;
                        context.Instance.Bucket = context.Data.Bucket;
                        context.Instance.Index = context.Data.Index;
                        context.Instance.Fields = context.Data.Fields;
                        context.Instance.FileCorrelationId = context.Data.CorrelationId;

                        await context.Raise(BeginCreating);
                    })
                );

            During(Creating,
                When(BeginCreating)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<CreateSpectrum>(new
                        {
                            Id = context.Instance.RecordId,
                            Index = context.Instance.Index,
                            Fields = context.Instance.Fields,
                            FileId = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket
                        });
                    }),
                When(SpectrumCreated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(EndCreating);
                    }),
                When(EndCreating)
                    .TransitionTo(Processed)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginProcessed);
                    })
            );

            During(Processed,
                Ignore(NodeStatusPersisted),
                Ignore(StatusPersisted),
                When(BeginProcessed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.RecordId,
                            UserId = context.Instance.UserId,
                            Status = RecordStatus.Processed
                        });
                    }),
				When(NodeStatusPersisted) 
					.ThenAsync(async context =>
					{
						await context.Raise(NodeStatusPersistenceDone);
					}),
				When(StatusPersisted) 
					.ThenAsync(async context =>
					{
						await context.Raise(StatusPersistenceDone);
					}),
		        When(AllPersisted)
			        .ThenAsync(async context =>
			        {
				        await context.Raise(EndProcessed);
			        }),
                When(EndProcessed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<SpectrumProcessed>(new
                        {
                            Id = context.Instance.RecordId,
                            FileId = context.Instance.FileId,
                            Index = context.Instance.Index,
                            Type = RecordType.Spectrum,
                            CorrelationId = context.Instance.FileCorrelationId,
                            UserId = context.Instance.UserId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public State Creating { get; private set; }
        public State Processed { get; private set; }

        public Event<RecordParsed> RecordParsed { get; private set; }
        public Event<SpectrumCreated> SpectrumCreated { get; private set; }
        public Event<SpectrumProcessed> SpectrumProcessed { get; private set; }
        public Event<StatusChanged> StatusChanged { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }

        internal Event BeginCreating { get; private set; }
        internal Event EndCreating { get; private set; }

        internal Event BeginProcessed { get; private set; }
        internal Event EndProcessed { get; private set; }
        
	    internal Event AllPersisted { get; set; }
	    internal Event NodeStatusPersistenceDone { get; set; }
	    internal Event StatusPersistenceDone { get; set; }
    }
}
