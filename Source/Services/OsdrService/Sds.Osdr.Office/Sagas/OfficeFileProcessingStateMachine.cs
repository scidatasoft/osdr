using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Domain;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.Office.Domain.Commands;
using Sds.Osdr.Office.Sagas.Commands;
using Sds.Osdr.Office.Sagas.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sds.Osdr.Generic.Domain.Events.Files;
using Sds.Osdr.Office.Domain.Events;

namespace Sds.Osdr.Office.Sagas
{
    public class Request
    {
        public object Command { get; set; }
        public DateTimeOffset DateStarted { get; }
        public DateTimeOffset DateFinished { get; }
        public bool Success { get; set; }
    }

    public class OfficeFileProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public Guid BlobId { get; set; }
        public string Bucket { get; set; }
        public Guid? PdfBlobId { get; set; }
        public string PdfBucket { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
		public int RecievedImages { get; set; } = 0;
        public IEnumerable<Property> Metadata { get; set; }
        public int AllProcessed { get; set; }
		public int AllParsingStepsDone { get; set; }
		public int AllPersisted { get; set; }
    }

    public static partial class PublishEndpointExtensions
    {
        public static async Task GenerateImage(this IPublishEndpoint endpoint, OfficeFileProcessingState state,
            int width, int height)
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

    public class OfficeFileProcessingStateMachine : MassTransitStateMachine<OfficeFileProcessingState>
    {
        public OfficeFileProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => ProcessFile, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ConvertedToPdf, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ConvertedToPdfFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => MetaExtractionFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FileProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => MetadataAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => PdfUpdated, x => x.CorrelateById(context => context.Message.Id));
	        Event(() => NodeStatusPersisted, x => x.CorrelateById(context => context.Message.Id));
	        Event(() => StatusPersisted, x => x.CorrelateById(context => context.Message.Id));

            CompositeEvent(() => EndProcessing, x => x.AllProcessed, ImagesGenerationDone, FileParseDone);
            CompositeEvent(() => FileParseDone, x => x.AllParsingStepsDone, ConvertedToPdf, PdfUpdated, MetadataAdded);
            CompositeEvent(() => AllPersisted, x => x.AllPersisted, StatusChanged, StatusPersistenceDone, NodeStatusPersistenceDone);
            
            Initially(
                When(ProcessFile)
                    .TransitionTo(Processing)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.FileId = context.Data.Id;
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
                        await context.CreateConsumeContext().Publish<ConvertToPdf>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            UserId = context.Instance.UserId
                        });

                        await context.CreateConsumeContext().Publish<ExtractMeta>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            UserId = context.Instance.UserId
                        });
                    }),

                When(ConvertedToPdf)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.PdfBlobId = context.Data.BlobId;
                        context.Instance.PdfBucket = context.Data.Bucket;

                        await context.CreateConsumeContext().Publish<UpdatePdf>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.PdfBlobId,
                            Bucket = context.Instance.PdfBucket,
                            UserId = context.Instance.UserId
                        });

                        await context.CreateConsumeContext().GenerateImage(context.Instance, 200, 200);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 500, 500);
                        await context.CreateConsumeContext().GenerateImage(context.Instance, 1000, 1000);
                    }),
                When(MetaExtracted)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.Metadata = context.Data.Meta;

                        await context.CreateConsumeContext().Publish<AddMetadata>(new
                        {
                            Id = context.Instance.FileId,
                            Metadata = context.Data.Meta,
                            UserId = context.Instance.UserId
                        });
                    }),
                When(MetaExtractionFailed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(MetadataAdded);

                    }),
                When(ImageGenerated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.CreateConsumeContext().Publish<AddImage>(new
                        {
                            Id = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            Image = new Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format,
                                context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height,
                                context.Data.Image.Exception)
                        });
                    }),
                When(ImageAdded)
                    .ThenAsync(async context =>
                    {
                        context.Instance.RecievedImages++;

                        if (context.Instance.RecievedImages == 3)
                        {
                            await context.Raise(ImagesGenerationDone);
                        }
                    }),
                When(ImageGenerationFailed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.RecievedImages++;

                        if (context.Instance.RecievedImages == 3)
                        {
                            await context.Raise(ImagesGenerationDone);
                        }

                        await context.Raise(BeginProcessed);
                    })
                    .TransitionTo(Processed),
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
                        await context.CreateConsumeContext().Publish<OfficeFileProcessed>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            CorrelationId = context.Instance.CorrelationId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public Event<ProcessOfficeFile> ProcessFile { get; private set; }
        public Event<ImageGenerated> ImageGenerated { get; private set; }
        public Event<ImageAdded> ImageAdded { get; private set; }
        public Event<ImageGenerationFailed> ImageGenerationFailed { get; private set; }
        public Event<ConvertedToPdf> ConvertedToPdf { get; private set; }
        public Event<ConvertToPdfFailed> ConvertedToPdfFailed { get; private set; }
        public Event<MetaExtracted> MetaExtracted { get; private set; }
        public Event<MetaExtractionFailed> MetaExtractionFailed { get; private set; }
        public Event<OfficeFileProcessed> FileProcessed { get; private set; }
        public Event<StatusChanged> StatusChanged { get; private set; }
        public Event<PdfBlobUpdated> PdfUpdated { get; private set; }
        public Event<MetadataAdded> MetadataAdded { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }
        
        public State Processing { get; private set; }
        public Event BeginProcessing { get; private set; }
        public Event EndProcessing { get; private set; }
        public State Processed { get; private set; }
        public Event BeginProcessed { get; private set; }
        public Event EndProcessed { get; private set; }
        
        public Event ImagesGenerationDone { get; private set; }
        public Event FileParseDone { get; set; }
        
	    internal Event AllPersisted { get; set; }
	    internal Event NodeStatusPersistenceDone { get; set; }
	    internal Event StatusPersistenceDone { get; set; }
    }
}
