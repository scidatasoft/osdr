using MassTransit;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.Cvsp;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Processing.CommandHandlers
{
    public class StandardizeValidateCommandHandler : IConsumer<ValidateStandardize>,
                                                    IConsumer<DeleteValidationStandardization>
    {
        private readonly Standardization standardization;
        private readonly Validation validation;
        private readonly IBlobStorage blobStorage;
        private readonly IIssuesConfig issuesConfig;

        public StandardizeValidateCommandHandler(Standardization standardization, Validation validation, IBlobStorage blobStorage, IIssuesConfig issuesConfig)
        {
            if (standardization == null)
                throw new ArgumentNullException(nameof(standardization));

            if (validation == null)
                throw new ArgumentNullException(nameof(validation));

            if (blobStorage == null)
                throw new ArgumentNullException(nameof(blobStorage));

            if (issuesConfig == null)
                throw new ArgumentNullException(nameof(issuesConfig));

            this.standardization = standardization;
            this.validation = validation;
            this.blobStorage = blobStorage;
            this.issuesConfig = issuesConfig;
        }

        public async Task Consume(ConsumeContext<ValidateStandardize> context)
        {
            try
            {
                using (var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket))
                {
                    StreamReader reader = new StreamReader(blob.GetContentAsStream());
                    string mol = reader.ReadToEnd();

                    var validResult = validation.Validate(mol);
                    var standardResult = standardization.Standardize(mol);

                    var newId = Guid.NewGuid();
                    var issues = standardResult.Issues.Concat(validResult.Issues);

                    var record = new StandardizedValidatedRecord
                    {
                        StandardizedId = newId,
                        Issues = IssuesResolver.ResolveIssues(issues, issuesConfig)
                    };
                    var bucket = context.Message.Id.ToString();

                    await blobStorage.AddFileAsync(newId, $"{newId}.mol", new MemoryStream(Encoding.UTF8.GetBytes(standardResult.Standardized)), "chemical/x-mdl-molfile", bucket);

                    await context.Publish<ValidatedStandardized>(new
                    {
                        Id = context.Message.Id,
                        Record = record,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });
                }
            }
            catch (Exception ex)
            {
                await context.Publish<StandardizationValidationFailed>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId,
                    Message = $"Blob with id {context.Message.BlobId} from bucket {context.Message.Bucket} can not be validated and standardized or not found. Error: {ex.Message}"
                });
            }
        }

        public async Task Consume(ConsumeContext<DeleteValidationStandardization> context)
        {
            await blobStorage.DeleteFileAsync(context.Message.Id);

            await context.Publish<StandardizationValidationDeleted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                CorrelationId = context.Message.CorrelationId
            });
        }
    }
}
