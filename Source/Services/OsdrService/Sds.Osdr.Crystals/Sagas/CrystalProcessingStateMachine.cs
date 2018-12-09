using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.CrystalFileParser.Domain.Events;
using Sds.Domain;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Crystals.Domain.Commands;
using Sds.Osdr.Crystals.Domain.Events;
using Sds.Osdr.Crystals.Sagas.Events;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.Osdr.Crystals.Sagas
{
    public class CrystalProcessingState : SagaStateMachineInstance, IVersionedSaga
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
        public int RecievedImages { get; set; } = 0;
        public IEnumerable<Field> Fields { get; set; }
        public int AllProcessed { get; set; }
		public int AllPersisted { get; set; }
    }

    public static partial class PublishEndpointExtensions
    {
        public static async Task GenerateImage(this IPublishEndpoint endpoint, CrystalProcessingState state, int width, int height)
        {
            await endpoint.Publish<Imaging.Domain.Commands.Jmol.GenerateImage>(new
            {
                Id = state.RecordId,
                UserId = state.UserId,
                BlobId = state.BlobId,
                Bucket = state.Bucket,
                CorrelationId = state.CorrelationId,
                Image = new Imaging.Domain.Models.Image()
                {
                    Id = NewId.NextGuid(),
                    Width = width,
                    Height = height,
                    Format = "png",
                    MimeType = "image/png"
                }
            });
        }
    }
    
    public class CrystalProcessingStateMachine : MassTransitStateMachine<CrystalProcessingState>
    {
        public CrystalProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => RecordParsed, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => CrystalCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => CrystalProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
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
                        await context.CreateConsumeContext().Publish<CreateCrystal>(new
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
                When(CrystalCreated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(EndCreating);
                        //  change status to processing
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
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        //  generate crystal images
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 300, 300);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 600, 600);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 1200, 1200);
                    }),
                When(ImageGenerated)
                    .ThenAsync(async context => 
					{
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.CreateConsumeContext().Publish<AddImage>(new
                        {
                            Id = context.Instance.RecordId,
                            UserId = context.Instance.UserId,
                            Image = new Generic.Domain.ValueObjects.Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
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
                    .ThenAsync(async context => {
						context.Instance.RecievedImages++;

                        if (context.Instance.RecievedImages == 3)
                        {
                            await context.Raise(EndProcessing);
                        }
                    }),
                When(ImageGenerationFailed)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;
                        
                        context.Instance.RecievedImages++;

						await context.CreateConsumeContext().Publish<ImageGenerationFailed>(new
						{
							Id = context.Instance.FileId,
							CorrelationId = context.Instance.FileCorrelationId,
                            Image = context.Data.Image,
                            Timestamp = DateTimeOffset.UtcNow
						});

                        if (context.Instance.RecievedImages == 3)
                        {
                            await context.Raise(EndProcessing);
                        }
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
                         await context.CreateConsumeContext().Publish<CrystalProcessed>(new
                         {
                             Id = context.Instance.RecordId,
                             FileId = context.Instance.FileId,
                             CorrelationId = context.Instance.FileCorrelationId,
                             Index = context.Instance.Index,
                             Type = RecordType.Crystal,
                             UserId = context.Instance.UserId,
                             Timestamp = DateTimeOffset.UtcNow
                         });
                     })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public Event<CrystalCreated> CrystalCreated { get; private set; }
        public Event<StatusChanged> StatusChanged { get; private set; }
        public Event<ImageAdded> ImageAdded { get; private set; }
        public Event<RecordParsed> RecordParsed { get; private set; }
        public Event<ImageGenerated> ImageGenerated { get; private set; }
        public Event<ImageGenerationFailed> ImageGenerationFailed { get; private set; }
        public Event<CrystalProcessed> CrystalProcessed { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }

        internal State Creating { get; private set; }
        internal Event BeginCreating { get; private set; }
        internal Event EndCreating { get; private set; }
        
        internal State Processing { get; private set; }
        internal Event BeginProcessing { get; private set; }
        internal Event EndProcessing { get; private set; }
        
        internal State Processed { get; private set; }
        internal Event BeginProcessed { get; private set; }
        internal Event EndProcessed { get; private set; }
        
	    internal Event AllPersisted { get; set; }
	    internal Event NodeStatusPersistenceDone { get; set; }
	    internal Event StatusPersistenceDone { get; set; }
    }
}
