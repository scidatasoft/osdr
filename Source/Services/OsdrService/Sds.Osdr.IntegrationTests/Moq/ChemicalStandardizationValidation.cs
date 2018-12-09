using MassTransit;
using Sds.ChemicalStandardizationValidation.Domain.Commands;
using Sds.ChemicalStandardizationValidation.Domain.Events;
using Sds.ChemicalStandardizationValidation.Domain.Models;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class ChemicalStandardizationValidation : IConsumer<Validate>
    {
        private readonly IBlobStorage _blobStorage;

        public ChemicalStandardizationValidation(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<Validate> context)
        {
            await context.Publish<Validated>(new
            {
                Id = context.Message.Id,
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                Record = new ValidatedRecord()
                {
                    Issues = new List<Issue>() { new Issue { Code = "Code", AuxInfo = "AuxInfo", Message = "Message", Severity = Severity.Information, Title = "Title" } }
                }
            });
        }
    }
}
