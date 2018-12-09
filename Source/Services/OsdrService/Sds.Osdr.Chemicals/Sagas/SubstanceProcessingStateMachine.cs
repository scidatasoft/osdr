using Automatonymous;
using MassTransit;
using MassTransit.MongoDbIntegration.Saga;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalProperties.Domain.Commands;
using Sds.ChemicalProperties.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.Domain;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Osdr.Chemicals.Domain.Commands;
using Sds.Osdr.Chemicals.Domain.Events;
using Sds.Osdr.Chemicals.Sagas.Events;
using Sds.Osdr.Domain.AccessControl;
using Sds.Osdr.Generic.Domain.ValueObjects;
using Sds.Osdr.RecordsFile.Domain;
using Sds.Osdr.RecordsFile.Domain.Commands.Records;
using Sds.Osdr.RecordsFile.Domain.Events.Records;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sds.Osdr.Chemicals.Sagas
{
    public class SubstanceProcessingState : SagaStateMachineInstance, IVersionedSaga
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

    public class SubstanceProcessingStateMachine : MassTransitStateMachine<SubstanceProcessingState>
    {
        private IAccessControl _accessControl = null;

        public SubstanceProcessingStateMachine(IAccessControl accessControl)
        {
            _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));

            InstanceState(x => x.CurrentState);

            Event(() => RecordParsed, x => x.CorrelateById(context => context.Message.Id).SelectId(context => context.Message.Id));
            Event(() => SubstanceCreated, x => x.CorrelateById(context => context.Message.Id));
            Event(() => StatusChanged, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => PropertiesAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => IssuesAdded, x => x.CorrelateById(context => context.Message.Id));
            Event(() => ImageGenerated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ImageGenerationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => SubstanceValidated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => SubstanceValidationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ChemicalPropertiesCalculated, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ChemicalPropertiesCalculationFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => SubstanceProcessed, x => x.CorrelateById(context => context.Message.CorrelationId));
	        Event(() => NodeStatusPersisted, x => x.CorrelateById(context => context.Message.Id));
	        Event(() => StatusPersisted, x => x.CorrelateById(context => context.Message.Id));

            CompositeEvent(() => EndProcessing, x => x.AllProcessed, SubstanceValidationDone, ImageGenerationDone, ChemicalPropertiesCalculationDone);
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
				        await context.CreateConsumeContext().Publish<CreateSubstance>(new
				        {
					        Id = context.Instance.RecordId,
					        Index = context.Instance.Index,
					        FileId = context.Instance.FileId,
					        UserId = context.Instance.UserId,
					        BlobId = context.Instance.BlobId,
					        Bucket = context.Instance.Bucket,
					        Fields = context.Instance.Fields
				        });
			        }),
		        When(SubstanceCreated)
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
                Ignore(StatusPersisted),
                Ignore(NodeStatusPersisted),
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

                        //  calculate chemical properties
                        if (_accessControl.IsServiceAvailable<CalculateChemicalProperties>())
                        {
                            await context.CreateConsumeContext().Publish<CalculateChemicalProperties>(new
                            {
                                Id = context.Instance.RecordId,
                                UserId = context.Instance.UserId,
                                BlobId = context.Instance.BlobId,
                                Bucket = context.Instance.Bucket,
                                CorrelationId = context.Instance.CorrelationId,
                            });
                        }
                        else
                        {
                            await context.Raise(ChemicalPropertiesCalculationDone);
                        }

                        //  validate substance
                        if (_accessControl.IsServiceAvailable<Validate>())
                        {
                            await context.CreateConsumeContext().Publish<Validate>(new
                            {
                                Id = context.Instance.RecordId,
                                UserId = context.Instance.UserId,
                                BlobId = context.Instance.BlobId,
                                Bucket = context.Instance.Bucket,
                                CorrelationId = context.Instance.CorrelationId,
                            });
                        }
                        else
                        {
                            await context.Raise(SubstanceValidationDone);
                        }
                    }),
				When(ImageGenerated)
					.ThenAsync(async context =>
					{
						if (context.Data.TimeStamp > context.Instance.Updated)
							context.Instance.Updated = context.Data.TimeStamp;

						context.Instance.Image = context.Data.Image;

						await context.CreateConsumeContext().Publish<AddImage>(new
						{
							Id = context.Instance.RecordId,
							UserId = context.Instance.UserId,
							Image = new Image(context.Instance.Bucket, context.Data.Image.Id, context.Data.Image.Format, context.Data.Image.MimeType, context.Data.Image.Width, context.Data.Image.Height, context.Data.Image.Exception)
						});

						if (context.Instance.Index < 10)
						{
							await context.CreateConsumeContext().Publish<ThumbnailGenerated>(new
							{
								Id = context.Data.Id,
                                Index = context.Instance.Index,
								BlobId = context.Data.BlobId,
								Bucket = context.Data.Bucket,
								TimeStamp = DateTimeOffset.UtcNow,
								UserId = context.Data.UserId,
								Image = context.Data.Image,
								CorrelationId = context.Instance.FileCorrelationId
							});
						}

                        //await context.Raise(ImageGenerationDone);
                    }),
				When(ImageGenerationFailed)
					.ThenAsync(async context => {
						if (context.Data.TimeStamp > context.Instance.Updated)
							context.Instance.Updated = context.Data.TimeStamp;

						context.Instance.Image = context.Data.Image;

                        await context.Raise(ImageGenerationDone);
                    }),
                When(ImageAdded)
                    .ThenAsync(async context => {
                        await context.Raise(ImageGenerationDone);
                    }),
				When(ChemicalPropertiesCalculated)
					.ThenAsync(async context => {
						if (context.Data.TimeStamp > context.Instance.Updated)
							context.Instance.Updated = context.Data.TimeStamp;

						await context.CreateConsumeContext().Publish<AddProperties>(new
						{
							Id = context.Instance.RecordId,
							UserId = context.Instance.UserId,
							Properties = context.Data.Result.Properties
						});

                        //await context.Raise(ChemicalPropertiesCalculationDone);
                    }),
				When(ChemicalPropertiesCalculationFailed)
					.ThenAsync(async context => {
						if (context.Data.TimeStamp > context.Instance.Updated)
							context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(ChemicalPropertiesCalculationDone);
                    }),
                When(PropertiesAdded)
                    .ThenAsync(async context => {
                        await context.Raise(ChemicalPropertiesCalculationDone);
                    }),
                When(SubstanceValidated)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.CreateConsumeContext().Publish<AddIssues>(new
                        {
                            Id = context.Instance.RecordId,
                            UserId = context.Instance.UserId,
                            Issues = context.Data.Record.Issues.Select(i => new Generic.Domain.ValueObjects.Issue()
                            {
                                Code = i.Code,
                                Title = i.Title,
                                Message = i.Message,
                                AuxInfo = i.AuxInfo,
                                Severity = (Severity)i.Severity
                            })
                        });

                        //await context.Raise(SubstanceValidationDone);
                    }),
                When(SubstanceValidationFailed)
                    .ThenAsync(async context => {
                        if (context.Data.TimeStamp > context.Instance.Updated)
                            context.Instance.Updated = context.Data.TimeStamp;

                        await context.Raise(SubstanceValidationDone);
                    }),
                When(IssuesAdded)
                    .ThenAsync(async context => {
                        await context.Raise(SubstanceValidationDone);
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
	                    await context.CreateConsumeContext().Publish<SubstanceProcessed>(new
	                    {
		                    Id = context.Instance.RecordId,
		                    FileId = context.Instance.FileId,
		                    Index = context.Instance.Index,
		                    Type = RecordType.Structure,
		                    CorrelationId = context.Instance.FileCorrelationId,
		                    UserId = context.Instance.UserId,
		                    Timestamp = DateTimeOffset.UtcNow
	                    });
			        })
                    .Finalize()
                );

            SetCompletedWhenFinalized();
        }

	    public Event<SubstanceCreated> SubstanceCreated { get; private set; }
        public Event<StatusChanged> StatusChanged { get; private set; }
        public Event<ImageAdded> ImageAdded { get; private set; }
        public Event<PropertiesAdded> PropertiesAdded { get; private set; }
        public Event<IssuesAdded> IssuesAdded { get; private set; }
        public Event<RecordParsed> RecordParsed { get; private set; }
        public Event<ImageGenerated> ImageGenerated { get; private set; }
        public Event<ImageGenerationFailed> ImageGenerationFailed { get; private set; }
        public Event<Validated> SubstanceValidated { get; private set; }
        public Event<ValidationFailed> SubstanceValidationFailed { get; private set; }
        public Event<ChemicalPropertiesCalculated> ChemicalPropertiesCalculated { get; private set; }
        public Event<ChemicalPropertiesCalculationFailed> ChemicalPropertiesCalculationFailed { get; private set; }
        public Event<SubstanceProcessed> SubstanceProcessed { get; private set; }
        public Event<NodeStatusPersisted> NodeStatusPersisted { get; private set; }
        public Event<StatusPersisted> StatusPersisted { get; private set; }

		State Creating { get; set; }
        Event BeginCreating { get; set; }
        Event EndCreating { get; set; }
		State Processing { get; set; }
        Event BeginProcessing { get; set; }
        Event EndProcessing { get; set; }
        State Processed { get; set; }
        Event BeginProcessed { get; set; }
        Event EndProcessed { get; set; }
	    
		Event SubstanceValidationDone { get; set; }
		Event ImageGenerationDone { get; set; }
		Event ChemicalPropertiesCalculationDone { get; set; }
	    
	    Event AllPersisted { get; set; }
        Event NodeStatusPersistenceDone { get; set; }
        Event StatusPersistenceDone { get; set; }
    }
}
