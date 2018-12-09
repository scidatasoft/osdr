using MassTransit;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.Cvsp;
using Sds.Cvsp.Compounds;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.ChemicalStandardizationValidation.Processing.CommandHandlers
{
    public class StandardizeCommandHandler : IConsumer<Standardize>,
                                             IConsumer<DeleteStandardization>
    {
        private readonly Standardization standardization;
        private readonly IBlobStorage blobStorage;
        private readonly IIssuesConfig issuesConfig;

        public StandardizeCommandHandler(Standardization standardization, IBlobStorage blobStorage, IIssuesConfig issuesConfig)
        {
            if (standardization == null)
                throw new ArgumentNullException(nameof(standardization));

            if (blobStorage == null)
                throw new ArgumentNullException(nameof(blobStorage));

            this.standardization = standardization;
            this.blobStorage = blobStorage;
            this.issuesConfig = issuesConfig;
        }

        public async Task Consume(ConsumeContext<Standardize> context)
        {
            try
            {
                using (var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket))
                {
                    StreamReader reader = new StreamReader(blob.GetContentAsStream());
                    string mol = reader.ReadToEnd();

                    var result = standardization.Standardize(mol);

                    var blobId = Guid.NewGuid();
                    var bucket = context.Message.Id.ToString();
                    await blobStorage.AddFileAsync(blobId, $"{blobId}.mol", new MemoryStream(Encoding.UTF8.GetBytes(result.Standardized)), "chemical/x-mdl-molfile", bucket);

                    await context.Publish<Standardized>(new
                    {
                        Id = context.Message.Id,
                        Record = new StandardizedRecord
                        {
                            StandardizedId = blobId,
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
                await context.Publish<StandardizationFailed>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId,
                    Message = $"Blob with id {context.Message.BlobId} from bucket {context.Message.Bucket} can not be standardized or not found. Error: {ex.Message}"
                });
            }
        }

        public async Task Consume(ConsumeContext<DeleteStandardization> context)
        {
            await blobStorage.DeleteFileAsync(context.Message.Id);

            await context.Publish<StandardizationDeleted>(new
            {
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                CorrelationId = context.Message.CorrelationId
            });
        }
    }
}
