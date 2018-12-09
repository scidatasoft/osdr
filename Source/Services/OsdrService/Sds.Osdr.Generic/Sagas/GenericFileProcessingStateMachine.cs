using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.Generic.Sagas.Commands;
using Sds.Osdr.Generic.Sagas.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.Osdr.Generic.Sagas
{
    public class GenericFileProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid FileId { get; set; }
        public Guid ParentId { get; set; }
        public Guid UserId { get; set; }
        public Guid BlobId { get; set; }
        public string Bucket { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public IList<Guid> Images { get; set; } = new List<Guid>();
        public int AllProcessed { get; set; }
        public int AllPersisted { get; set; }
    }

    public static partial class PublishEndpointExtensions
    {
        public static async Task GenerateImage(this IPublishEndpoint endpoint, GenericFileProcessingState state, int width, int height)
        {
            await endpoint.Publish<GenerateImage>(new
            {
                Id = state.FileId,
                UserId = state.UserId,
                BlobId = state.BlobId,
                Bucket = state.Bucket,
                CorrelationId = state.CorrelationId,
                Image = new Imaging.Domain.Models.Image()
                {
                    Id = NewId.NextGuid(),
                    Width = width,
                    Height = height,
                    Format = "PNG",
                    MimeType = "image/png"
                }
            });
        }
    }

    public class GenericFileProcessingStateMachine : MassTransitStateMachine<GenericFileProcessingState>
    {
        public GenericFileProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => ProcessGenericFile, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => FileProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => NodeStatusPersisted, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusPersisted, x => x.CorrelateById(context => context.Message.Id));

            CompositeEvent(() => AllPersisted, x => x.AllPersisted, StatusChanged, StatusPersistenceDone, NodeStatusPersistenceDone);
            
            Initially(
                When(ProcessGenericFile)
                    .TransitionTo(Processing)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"GenericFile: ProcessGenericFile {context.Data.Id}");

                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.FileId = context.Data.Id;
                        context.Instance.ParentId = context.Data.ParentId;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.BlobId = context.Data.BlobId;
                        context.Instance.Bucket = context.Data.Bucket;

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
                            Id = context.Instance.FileId,
                            Status = FileStatus.Processing,
                            UserId = context.Instance.UserId
                        });
                    }),
                When(StatusChanged)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 300, 300);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 600, 600);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 1200, 1200);
                    }),
                When(ImageGenerated)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.CreateConsumeContext().Publish<AddImage>(new
                        {
                            Id = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            Image = new Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
                        });
                    }),
                When(ImageGenerationFailed)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Images.Add(context.Data.Image.Id);

                        if (context.Instance.Images.Count == 3)
                        {
                            await context.Raise(EndProcessing);
                        }
                    }),
                When(ImageAdded)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Images.Add(context.Data.Image.Id);

                        if (context.Instance.Images.Count == 3)
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
                            Id = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            Status = FileStatus.Processed
                        });
                    }),
				When(NodeStatusPersisted) 
					.ThenAsync(async context =>
					{
                        if(context.Data.Status != FileStatus.Processing)
                        {
                            await context.Raise(NodeStatusPersistenceDone);
                        }
                    }),
				When(StatusPersisted) 
					.ThenAsync(async context =>
					{
                        if (context.Data.Status != FileStatus.Processing)
                        {
                            await context.Raise(StatusPersistenceDone);
                        }
					}),
		        When(AllPersisted)
			        .ThenAsync(async context =>
			        {
				        await context.Raise(EndProcessed);
			        }),
                When(EndProcessed)
                    .ThenAsync(async context =>
                    {
                        Log.Debug($"GenericFile: EndProcessed {context.Instance.FileId}");

                        await context.CreateConsumeContext().Publish<FileProcessed>(new
                        {
                            Id = context.Instance.FileId,
                            ParentId = context.Instance.ParentId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        Event<ProcessGenericFile> ProcessGenericFile { get; set; }
        Event<ImageGenerated> ImageGenerated { get; set; }
        Event<ImageGenerationFailed> ImageGenerationFailed { get; set; }
        Event<ImageAdded> ImageAdded { get; set; }
        Event<FileProcessed> FileProcessed { get; set; }
        Event<StatusChanged> StatusChanged { get; set; }
        Event<NodeStatusPersisted> NodeStatusPersisted { get; set; }
        Event<StatusPersisted> StatusPersisted { get; set; }

        State Processing { get; set; }
        Event BeginProcessing { get; set; }
        Event EndProcessing { get; set; }
        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }
        
	    Event AllPersisted { get; set; }
	    Event NodeStatusPersistenceDone { get; set; }
	    Event StatusPersistenceDone { get; set; }
    }
}