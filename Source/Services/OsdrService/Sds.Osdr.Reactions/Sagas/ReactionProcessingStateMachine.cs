using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Domain;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.Reactions.Domain.Commands;
using Sds.Osdr.Reactions.Domain.Events;
using Sds.Osdr.Reactions.Sagas.Events;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using Sds.ReactionFileParser.Domain.Events;
using System;
using System.Collections.Generic;

namespace Sds.Osdr.Reactions.Sagas
{
    public class ReactionProcessingState : SagaStateMachineInstance, IVersionedSaga
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
        public Imaging.Domain.Models.Image Image { get; set; }
        public IEnumerable<Field> Fields { get; set; }
        public int AllProcessed { get; set; }
		public int AllPersisted { get; set; }
    }

    public class ReactionProcessingStateMachine : MassTransitStateMachine<ReactionProcessingState>
    {
        public ReactionProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => RecordParsed, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => ReactionCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
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
                        await context.CreateConsumeContext().Publish<CreateReaction>(new
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
                When(ReactionCreated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(EndCreating);
                    }),
                When(EndCreating)
                    .TransitionTo(Processing)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginProcessing);
                    })
                );

            During(Processing,
                Ignore(NodeStatusPersisted),
                Ignore(StatusPersisted),
                When(BeginProcessing)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.RecordId,
                            Status = RecordStatus.Processing,
                            UserId = context.Instance.UserId
                        });
                    }),
                When(StatusChanged)
                    .ThenAsync(async context =>
                    {
                        //  generate substance image
                        await context.CreateConsumeContext().Publish<GenerateImage>(new
                        {
                            Id = context.Instance.RecordId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            Image = new Imaging.Domain.Models.Image()
                            {
                                Id = NewId.NextGuid(),
                                Width = 200,
                                Height = 200,
                                Format = "SVG",
                                MimeType = "image/svg+xml"
                            }
                        });
                    }),
                When(ImageGenerated)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Image = context.Data.Image;

                        await context.CreateConsumeContext().Publish<AddImage>(new
                        {
                            Id = context.Instance.RecordId,
                            UserId = context.Instance.UserId,
                            Image = new Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
                        });

                        if (context.Instance.Index == 0)
                        {
                            await context.CreateConsumeContext().Publish<ImageGenerated>(new
                            {
                                Id = context.Data.Id,
                                BlobId = context.Data.BlobId,
                                Bucket = context.Data.Bucket,
                                TimeStamp = DateTimeOffset.UtcNow,
                                UserId = context.Data.UserId,
                                Image = context.Data.Image,
                                CorrelationId = context.Instance.FileCorrelationId
                            });
                        }
                    }),
                When(ImageAdded)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(EndProcessing);
                    }),
                When(ImageGenerationFailed)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Image = context.Data.Image;

                        await context.Raise(EndProcessing);
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
                        await context.CreateConsumeContext().Publish<ReactionProcessed>(new
                        {
                            Id = context.Instance.RecordId,
                            FileId = context.Instance.FileId,
                            Index = context.Instance.Index,
                            Type = RecordType.Reaction,
                            CorrelationId = context.Instance.FileCorrelationId,
                            UserId = context.Instance.UserId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public Event<RecordParsed> RecordParsed { get; private set; }
        public Event<ReactionCreated> ReactionCreated { get; private set; }
        public Event<StatusChanged> StatusChanged { get; private set; }
        public Event<ImageGenerated> ImageGenerated { get; private set; }
        public Event<ImageGenerationFailed> ImageGenerationFailed { get; private set; }
        public Event<ImageAdded> ImageAdded { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }

        public State Creating { get; private set; }
        public Event BeginCreating { get; set; }
        public Event EndCreating { get; set; }
		internal State Processing { get; private set; }
        internal Event BeginProcessing { get; private set; }
        internal Event EndProcessing { get; private set; }
        public State Processed { get; private set; }
        public Event BeginProcessed { get; set; }
        public Event EndProcessed { get; set; }
        
	    internal Event AllPersisted { get; set; }
	    internal Event NodeStatusPersistenceDone { get; set; }
	    internal Event StatusPersistenceDone { get; set; }
    }
}
