using Automatonymous;
using MassTransit.MongoDbIntegration.Saga;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.Osdr.Chemicals.Sagas.Commands;
using Sds.Osdr.Chemicals.Sagas.Events;
using Sds.Osdr.Generic.Domain;
using Sds.Osdr.Generic.Domain.Commands.Files;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.RecordsFile.Domain.Commands.Files;
using Sds.Osdr.RecordsFile.Domain.Events.Files;
using Sds.Osdr.RecordsFile.Sagas.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using NodeStatusPersisted = Sds.Osdr.Generic.Domain.Events.Files.NodeStatusPersisted;
using StatusPersisted = Sds.Osdr.Generic.Domain.Events.Files.StatusPersisted;
using Sds.MetadataStorage.Domain.Commands;
using Sds.MetadataStorage.Domain.Events;

namespace Sds.Osdr.Chemicals.Sagas
{
    public class ChemicalFileProcessingState : SagaStateMachineInstance, IVersionedSaga
    {
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public Guid BlobId { get; set; }
        public string Bucket { get; set; }
        public string CurrentState { get; set; }
        public Guid CorrelationId { get; set; }
        public int Version { get; set; }
        public Guid _id { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Updated { get; set; }
        public FileStatus FileParseStatus { get; set; }
        public string FileParseError { get; set; }
        public long ProcessedRecords { get; set; }
        public long FailedRecords { get; set; }
        /// <summary>
        /// Number of total records inside the file
        /// </summary>
        public long TotalParsedRecords { get; set; }
        /// <summary>
        /// Number of successfully parsed records
        /// </summary>
        public long SuccessParsedRecords { get; set; }
        /// <summary>
        /// Number of invalid records that crushed during file parsing
        /// </summary>
        public long FailParsedRecords { get; set; }
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfArrays)]
        public Dictionary<long, Imaging.Domain.Models.Image> Thumbnails { get; set; }
        public int AllProcessed { get; set; }
        public int AllParsingStepsDone { get; set; }
        public int AllPersisted { get; set; }
        public int AllPostProcessed { get; set; }
    }

    public class ChemicalFileProcessingStateMachine : MassTransitStateMachine<ChemicalFileProcessingState>
    {
        public ChemicalFileProcessingStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => ProcessFile, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => FileParsed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FileParseFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ThumbnailGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => SubstanceProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            //Event(() => SubstanceProcessingFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => FileProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => AggregatedPropertiesAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => InvalidRecordProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => TotalRecordsUpdated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => FieldsAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => NodeStatusPersisted, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusPersisted, x => x.CorrelateById(context => context.Message.Id));
            Event(() => MetadataGenerated, x => x.CorrelateById(context => context.Message.Id));

            CompositeEvent(() => EndProcessing, x => x.AllProcessed, ThumbnailGenerationDone, FileParseDone, AllRecordsProcessingDone);
            CompositeEvent(() => FileParseDone, x => x.AllParsingStepsDone, UpdateTotalRecordsDone, AddFieldsDone);
            CompositeEvent(() => AllPersisted, x => x.AllPersisted, StatusChanged, StatusPersistenceDone, NodeStatusPersistenceDone);
            CompositeEvent(() => EndPostProcessing, x => x.AllPostProcessed, AggregatedPropertiesAdded, MetadataGenerated);

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
                        context.Instance.FailParsedRecords = 0;
                        context.Instance.SuccessParsedRecords = 0;
                        context.Instance.FileParseStatus = FileStatus.Parsing;
                        context.Instance.Thumbnails = new Dictionary<long, Imaging.Domain.Models.Image>();

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
                        await context.CreateConsumeContext().Publish<ParseFile>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            UserId = context.Instance.UserId,
                            CorrelationId = context.Instance.CorrelationId
                        });
                    }),
                When(FileParsed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.TotalParsedRecords = context.Data.TotalRecords;
                        context.Instance.SuccessParsedRecords = context.Data.ParsedRecords;
                        context.Instance.FailParsedRecords = context.Data.FailedRecords;
                        context.Instance.FileParseStatus = FileStatus.Parsed;

                        if (context.Instance.TotalParsedRecords > 0)
                        {
                            await context.CreateConsumeContext().Publish<UpdateTotalRecords>(new
                            {
                                Id = context.Instance.FileId,
                                UserId = context.Instance.UserId,
                                //ParsedRecords = context.Instance.SuccessParsedRecords,
                                //FailedRecords = context.Instance.FailParsedRecords,
                                TotalRecords = context.Instance.TotalParsedRecords
                            });
                        }
                        else
                        {
                            await context.Raise(UpdateTotalRecordsDone);
                        }

                        if (context.Data.Fields?.Any() ?? false)
                        {
                            await context.CreateConsumeContext().Publish<AddFields>(new
                            {
                                Id = context.Instance.FileId,
                                context.Instance.UserId,
                                context.Data.Fields
                            });
                        }
                        else
                        {
                            await context.Raise(AddFieldsDone);
                        }

                        if (context.Instance.Thumbnails.Count == Math.Min(10, context.Instance.TotalParsedRecords))
                        {
                            await context.Raise(AllThumbnailsRecived);
                        }

                        if (context.Instance.TotalParsedRecords == context.Instance.ProcessedRecords + context.Instance.FailedRecords)
                        {
                            await context.Raise(AllRecordsProcessingDone);
                        }
                    }),
                When(TotalRecordsUpdated)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(UpdateTotalRecordsDone);
                    }),
                When(FieldsAdded)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(AddFieldsDone);
                    }),
                When(FileParseFailed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.TotalParsedRecords = context.Data.TotalRecords;
                        context.Instance.SuccessParsedRecords = context.Data.ParsedRecords;
                        context.Instance.FailParsedRecords = context.Data.FailedRecords;
                        context.Instance.FileParseStatus = FileStatus.Failed;
                        context.Instance.FileParseError = context.Data.Message;

                        if (context.Instance.Thumbnails.Count == Math.Min(10, context.Instance.TotalParsedRecords))
                        {
                            await context.Raise(AllThumbnailsRecived);
                        }

                        if (context.Instance.TotalParsedRecords == context.Instance.ProcessedRecords + context.Instance.FailedRecords)
                        {
                            await context.Raise(AllRecordsProcessingDone);
                        }

                        await context.Raise(FileParseDone);
                    }),
                When(ThumbnailGenerated)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        if (context.Data.Index < 10)
                        {
                            context.Instance.Thumbnails[context.Data.Index] = context.Data.Image;

                            if (context.Instance.Thumbnails.Count == Math.Min(10, context.Instance.TotalParsedRecords))
                            {
                                await context.Raise(AllThumbnailsRecived);
                            }
                        }
                    }),
                When(SubstanceProcessed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.ProcessedRecords++;

                        if (context.Instance.TotalParsedRecords == context.Instance.ProcessedRecords + context.Instance.FailedRecords)
                        {
                            await context.Raise(AllRecordsProcessingDone);
                        }
                    }),
                //When(SubstanceProcessingFailed)
                //    .Then(context =>
                //    {
                //        if (context.Data.TimeStamp > context.Instance.Updated)
                //            context.Instance.Updated = context.Data.TimeStamp;

                //        context.Instance.FailedRecords++;
                //    })
                //    .If(context => context.Instance.IsProcessed(), x => x
                //        .ThenAsync(async context => await context.CreateConsumeContext().ChemicalFileProcessed(context.Instance))
                //        .TransitionTo(PostProcessing)
                //    ),
                When(InvalidRecordProcessed)
                    .ThenAsync(async context =>
                    {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        context.Instance.FailedRecords++;

                        if (context.Data.Index < 10)
                        {
                            context.Instance.Thumbnails[context.Data.Index] = null;

                            if (context.Instance.Thumbnails.Count == Math.Min(10, context.Instance.TotalParsedRecords))
                            {
                                await context.Raise(AllThumbnailsRecived);
                            }
                        }

                        if (context.Instance.TotalParsedRecords == context.Instance.ProcessedRecords + context.Instance.FailedRecords)
                        {
                            await context.Raise(AllRecordsProcessingDone);
                        }
                    }),
                When(AllThumbnailsRecived)
                    .ThenAsync(async context =>
                    {
                        for (var i = 0; i < context.Instance.Thumbnails.Count; i++)
                        {
                            var image = context.Instance.Thumbnails[i];
                            if (image != null)
                            {
                                await context.CreateConsumeContext().Publish<AddImage>(new
                                {
                                    Id = context.Instance.FileId,
                                    UserId = context.Instance.UserId,
                                    Image = new Image(context.Instance.Bucket, image.Id, image.Format, image.MimeType, image.Width, image.Height, image.Exception)
                                });

                                break;
                            }
                        }

                        await context.Raise(ThumbnailGenerationDone);
                    }),
                When(EndProcessing)
                    .TransitionTo(PostProcessing)
                    .ThenAsync(async context =>
                    {
                        await context.Raise(BeginPostProcessing);
                    })
                );

            During(PostProcessing,
                Ignore(NodeStatusPersisted),
                Ignore(StatusPersisted),
                When(BeginPostProcessing)
                    .ThenAsync(async context =>
                    {
                        if (context.Instance.ProcessedRecords > 0)
                        {
                            await context.CreateConsumeContext().Publish<AggregateProperties>(new
                            {
                                Id = context.Instance.FileId,
                                CorrelationId = context.Instance.CorrelationId,
                                UserId = context.Instance.UserId
                            });

                            await context.CreateConsumeContext().Publish<GenerateMetadata>(new
                            {
                                context.Instance.FileId
                            });
                        }
                        else
                        {
                            await context.Raise(EndPostProcessing);
                        }
                    }),

                When(EndPostProcessing)
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
                        FileStatus status = FileStatus.Processed;

                        if (context.Instance.ProcessedRecords == 0)
                        {
                            status = FileStatus.Failed;
                        }

                        await context.CreateConsumeContext().Publish<ChangeStatus>(new
                        {
                            Id = context.Instance.FileId,
                            UserId = context.Instance.UserId,
                            Status = status
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
                        await context.CreateConsumeContext().Publish<ChemicalFileProcessed>(new
                        {
                            Id = context.Instance.FileId,
                            BlobId = context.Instance.BlobId,
                            Bucket = context.Instance.Bucket,
                            FailedRecords = context.Instance.FailedRecords,
                            ProcessedRecords = context.Instance.ProcessedRecords,
                            CorrelationId = context.Instance.CorrelationId,
                            Timestamp = DateTimeOffset.UtcNow
                        });
                    })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

        public Event<MetadataGenerated> MetadataGenerated { get; private set; }
        public Event<ProcessChemicalFile> ProcessFile { get; private set; }
        public Event<FileParsed> FileParsed { get; private set; }
        public Event<FileParseFailed> FileParseFailed { get; private set; }
        public Event<ThumbnailGenerated> ThumbnailGenerated { get; private set; }
        public Event<TotalRecordsUpdated> TotalRecordsUpdated { get; private set; }
        public Event<FieldsAdded> FieldsAdded { get; private set; }
        public Event<SubstanceProcessed> SubstanceProcessed { get; private set; }
        //public Event<SubstanceProcessingFailed> SubstanceProcessingFailed { get; private set; }
        public Event<ChemicalFileProcessed> FileProcessed { get; private set; }
        public Event<AggregatedPropertiesAdded> AggregatedPropertiesAdded { get; private set; }
        public Event<Generic.Domain.Events.Files.StatusChanged> StatusChanged { get; private set; }
        public Event<InvalidRecordProcessed> InvalidRecordProcessed { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }

        State Processing { get; set; }
        Event BeginProcessing { get; set; }
        Event EndProcessing { get; set; }
        State PostProcessing { get; set; }
        Event BeginPostProcessing { get; set; }
        Event EndPostProcessing { get; set; }
        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }

        Event AllThumbnailsRecived { get; set; }
        Event ThumbnailGenerationDone { get; set; }
        Event AllRecordsProcessingDone { get; set; }
        Event FileParseDone { get; set; }
        Event UpdateTotalRecordsDone { get; set; }
        Event AddFieldsDone { get; set; }

        Event AllPersisted { get; set; }
        Event NodeStatusPersistenceDone { get; set; }
        Event StatusPersistenceDone { get; set; }
    }
}
