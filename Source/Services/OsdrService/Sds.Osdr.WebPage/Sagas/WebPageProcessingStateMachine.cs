using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Chemicals.Sagas.Events;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.Events;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.WebPage.Domain.Commands;
using Sds.Osdr.WebPage.Domain.Events;
using Sds.Osdr.WebPage.Sagas.Commands;
using Sds.Osdr.WebPage.Sagas.Events;
using Sds.WebImporter.Domain;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Osdr.WebPage.Sagas
{
    public class WebPageProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid PageId { get; set; }
        public Guid UserId { get; set; }
        public string Url { get; set; }
        public Guid PdfFileId { get; set; }
        public Guid? JsonBlobId { get; set; }
        public string PdfBucket { get; set; }
        public string JsonBucket { get; set; }
        public Guid? PdfBlobId { get; set; }
        public Guid CorrelationId { get; set; }
        public string Bucket { get; set; }
        public long? TotalRecords { get; set; }
        public long ProcessedRecords { get; set; }
        public string CurrentState { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public IList<Imaging.Domain.Models.Image> Images { get; set; } = new List<Imaging.Domain.Models.Image>();
        public Guid ParentId { get; set; }
        public bool isPageProcessed { get; set; }

    }

    public static class WebPageProcessingStateExtensions
    {
        public static bool IsProcessed(this WebPageProcessingState state)
        {
            return
                state.Images.Count >= 3 && state.isPageProcessed &&
                ((state.TotalRecords == state.ProcessedRecords && state.ProcessedRecords != 0 && state.TotalRecords != null)
                ||(state.JsonBlobId==null && state.JsonBucket==null));
        }

        public static bool IsContainsChemicalData(this WebPageProcessingState state)
        {
            return state.JsonBlobId != null && state.JsonBucket != null;
        }
    }

    public static partial class PublishEndpointExtensions
    {
        public static async Task WebPageProcessFinished(this IPublishEndpoint endpoint, WebPageProcessingState state)
        {
            await endpoint.Publish<WebPageUploadFinished>(new
            {
                Id = state.PageId,
                CorrelationId = state.CorrelationId,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    public class WebPageProcessingStateMachine : MassTransitStateMachine<WebPageProcessingState>
    {
        public WebPageProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => UploadWebPage, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => PdfGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => PdfGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageParsed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageParseFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageProcessFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => WebPageUploadFinished, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => SubstanceProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));

            Initially(
                When(UploadWebPage)
                    .ThenAsync(async context =>
                    {
                        context.Instance.Created = DateTimeOffset.UtcNow;
                        context.Instance.Updated = DateTimeOffset.UtcNow;
                        context.Instance.PageId = context.Data.Id;
                        context.Instance.UserId = context.Data.UserId;
                        context.Instance.Bucket = context.Data.Bucket;
                        context.Instance.Url = context.Data.Url;
                        context.Instance.ParentId = context.Data.ParentId;
                        context.Instance.TotalRecords = null;
                        context.Instance.JsonBlobId = null;
                        context.Instance.JsonBucket = null;
                        context.Instance.isPageProcessed = false;

                        await context.CreateConsumeContext().Publish(new ProcessingStarted(context.Instance.PageId, context.Instance.UserId, context.Instance.ParentId, "File", context.Data.GetType().Name, context.Data));

                        await context.CreateConsumeContext().Publish<GeneratePdfFromHtml>(new
                        {
                            Id = context.Instance.PageId,
                            CorrelationId = context.Instance.CorrelationId,
                            UserId = context.Instance.UserId,
                            Bucket = context.Instance.Bucket,
                            Url = context.Instance.Url
                        });
                    })
                    .TransitionTo(Uploading)
                );

            During(Uploading,
                When(PdfGenerated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.PdfFileId = context.Data.Id;
                        context.Instance.PdfBlobId = (Guid)context.Data.BlobId;

                        var name = context.Data.Title;
                        if(name == "" || name == null)
                        {
                            var  correctUrl = Uri.UnescapeDataString(context.Instance.Url);
                            name = Path.GetFileName(correctUrl) == "" ? context.Instance.Url : Path.GetFileName(correctUrl);
                        }
                        
                        await context.CreateConsumeContext().Publish<CreateWebPage>(new
                        {
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            ParentId = context.Instance.ParentId,
                            Name = name,
                            FileId = context.Instance.PdfFileId,
                            Status = FileStatus.Processing,
                            Bucket = context.Instance.Bucket,
                            BlobId = context.Data.BlobId,
                            Lenght = context.Data.Lenght,
                            Md5 = context.Data.Md5,
                            Url = context.Instance.Url,
                        });
                    }).TransitionTo(Processing),
              When(PdfGenerationFailed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish(new OperationFailed(context.Instance.PageId, context.Instance.UserId,
                            context.Instance.ParentId, "WebPage", context.Data));

                        await context.CreateConsumeContext().Publish<WebPageUploadFailed>(new
                        {
                            Id = context.Instance.PageId,
                            CorrelationId = context.Instance.CorrelationId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    }).TransitionTo(Processed)
                );

            During(Processing,
                When(WebPageCreated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.CreateConsumeContext().Publish<GenerateImage>(new
                        {
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Instance.PdfBlobId,
                            Bucket = context.Instance.Bucket,
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
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Instance.PdfBlobId,
                            Bucket = context.Instance.Bucket,
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
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            BlobId = context.Instance.PdfBlobId,
                            Bucket = context.Instance.Bucket,
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

                        await context.CreateConsumeContext().Publish<ProcessWebPage>(new
                        {
                            CorrelationId = context.Instance.CorrelationId,
                            Bucket = context.Instance.Bucket,
                            Url = context.Instance.Url,
                            Id = context.Instance.Bucket,
                            UserId = context.Instance.UserId
                        });
                    }),

                When(WebPageProcessed)
                    .ThenAsync(async context =>
                    {
                        context.Instance.JsonBlobId = context.Data.BlobId;
                        context.Instance.JsonBucket = context.Data.Bucket;
                        context.Instance.isPageProcessed = true;

                        if (context.Data.BlobId != null && context.Data.Bucket != null)
                        {
                            await context.CreateConsumeContext().Publish<UpdateWebPage>(new
                            {
                                Id = context.Instance.PageId,
                                CorrelationId = context.Instance.CorrelationId,
                                Bucket = context.Instance.Bucket,
                                JsonBlobId = context.Instance.PdfBlobId,
                                UserId = context.Instance.UserId
                            });

                            context.Instance.ProcessedRecords = 0;

                            await context.CreateConsumeContext().Publish<ParseWebPage>(new
                            {
                                CorrelationId = context.Data.CorrelationId,
                                Bucket = context.Data.Bucket,
                                BlobId = (Guid)context.Data.BlobId,
                                Id = context.Instance.PageId,
                                UserId = context.Instance.UserId
                            });
                        }
                    })
                    .If(context => context.Instance.IsProcessed(), x => x
                        .ThenAsync(async context => await context.CreateConsumeContext().WebPageProcessFinished(context.Instance))
                        .TransitionTo(Processed)
                    ),

                When(WebPageParseFailed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish(new OperationFailed(context.Instance.PageId, context.Instance.UserId,
                            context.Instance.ParentId, "WebPage", context.Data));
                    })
                    .If(context => context.Instance.IsProcessed(), x => x
                        .ThenAsync(async context => await context.CreateConsumeContext().WebPageProcessFinished(context.Instance))
                        .TransitionTo(Processed)
                    ),

                When(WebPageParsed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.TotalRecords = context.Data.TotalRecords;

                        await context.CreateConsumeContext().Publish<UpdateTotalWebRecords>(new
                        {
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            TotalRecords = context.Data.TotalRecords
                        });
                    })
                    .If(context => context.Instance.IsProcessed(), x => x
                        .ThenAsync(async context => await context.CreateConsumeContext().WebPageProcessFinished(context.Instance))
                        .TransitionTo(Processed)
                    ),

                When(ImageGenerated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        if(context.Data.Image.Format.ToLower()== "png" && !context.Instance.Images.Contains(context.Data.Image) 
                        && (context.Data.Image.Width == 300 || context.Data.Image.Width == 600 || context.Data.Image.Width == 1200))
                        {
                            context.Instance.Images.Add(context.Data.Image);

                            await context.CreateConsumeContext().Publish<AddImage>(new
                            {
                                Id = context.Instance.PageId,
                                UserId = context.Instance.UserId,
                                Image = new Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
                            });
                        } 
                    })
                    .If(context => context.Instance.IsProcessed(), x => x
                        .ThenAsync(async context => await context.CreateConsumeContext().WebPageProcessFinished(context.Instance))
                        .TransitionTo(Processed)
                    ),
                When(SubstanceProcessed)
                    .Then(context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.ProcessedRecords++;
                    })
                    .If(context => context.Instance.IsProcessed(), x => x
                        .ThenAsync(async context => await context.CreateConsumeContext().WebPageProcessFinished(context.Instance))
                        .TransitionTo(Processed)
                    )
                );

            During(Processed,
                When(WebPageUploadFinished)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.PageId,
                            UserId = context.Instance.UserId,
                            Status = FileStatus.Processed
                        });

                        await context.CreateConsumeContext().Publish(new ProcessingFinished(context.Instance.PageId, context.Instance.UserId, context.Instance.ParentId, "File", context.Data.GetType().Name, context.Data));
                    })
                    .Finalize(),

               When(WebPageUploadFailed)
                    .ThenAsync(async context =>
                    {
                        await context.CreateConsumeContext().Publish(new ProcessingFailed(context.Instance.PageId, context.Instance.UserId, context.Instance.ParentId, "File", context.Instance));
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public State Uploading { get; private set; }
        public State Processing { get; private set; }
        public State Processed { get; private set; }

        public Event<UploadWebPage> UploadWebPage { get; private set; }
        public Event<PdfGenerationFailed> PdfGenerationFailed { get; private set; }
        public Event<ImageGenerated> ImageGenerated { get; private set; }
        public Event<PdfGenerated> PdfGenerated { get; private set; }
        public Event<WebPageParsed> WebPageParsed { get; private set; }
        public Event<WebPageProcessed> WebPageProcessed { get; private set; }
        public Event<WebPageParseFailed> WebPageParseFailed { get; private set; }
        public Event<WebPageUploadFinished> WebPageUploadFinished { get; private set; }
        public Event<SubstanceProcessed> SubstanceProcessed { get; private set; }
        public Event<WebPageProcessFailed> WebPageProcessFailed { get; private set; }
        public Event<WebPageUploadFailed> WebPageUploadFailed { get; private set; }
        public Event<WebPageCreated> WebPageCreated { get; private set; }
    }
}
