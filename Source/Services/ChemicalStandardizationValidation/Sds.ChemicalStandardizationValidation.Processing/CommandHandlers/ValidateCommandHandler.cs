using MassTransit;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.Cvsp;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Processing.CommandHandlers
{
    public class ValidateCommandHandler : IConsumer<Validate>,
                                        IConsumer<DeleteValidation>
    {
        private readonly Validation validation;
        private readonly IBlobStorage blobStorage;
        private readonly IIssuesConfig issuesConfig;

        public ValidateCommandHandler(Validation validation, IBlobStorage blobStorage, IIssuesConfig issuesConfig)
        {
            if (validation == null)
                throw new ArgumentNullException(nameof(validation));

            if (blobStorage == null)
                throw new ArgumentNullException(nameof(blobStorage));

            this.validation = validation;
            this.blobStorage = blobStorage;
            this.issuesConfig = issuesConfig;
        }

        public async Task Consume(ConsumeContext<Validate> context)
        {
            try
            {
                using (var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket))
                {
                    StreamReader reader = new StreamReader(blob.GetContentAsStream());
                    string mol = reader.ReadToEnd();
                
                    var result = validation.Validate(mol);

                    await context.Publish<Validated>(new
                    {
                        Id = context.Message.Id,
                        Record = new ValidatedRecord
                        {
                            Issues = IssuesResolver.ResolveIssues(result.Issues, issuesConfig)
                        },
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });
                }
            }
            catch (Exception ex)
            {
                await context.Publish<ValidationFailed>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId,
                    Message = $"Blob with id {context.Message.BlobId} from bucket {context.Message.Bucket} cannot be validated or not found. Error: {ex.Message}"
                });
            }

        }

        public async Task Consume(ConsumeContext<DeleteValidation> context)
        {
            await context.Publish<ValidationDeleted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                CorrelationId = context.Message.CorrelationId
            });
        }
    }
}

