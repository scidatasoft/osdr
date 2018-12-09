using MassTransit;
using Sds.Domain;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Osdr.Domain.IntegrationTests.Moq
{
    public class OfficeProcessor : IConsumer<ConvertToPdf>,
                                   IConsumer<ExtractMeta>
    {
        private readonly IBlobStorage _blobStorage;

        public OfficeProcessor(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ConvertToPdf> context)
        {
            var blobId = Guid.NewGuid();

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Abdelaziz A Full_manuscript.pdf");

            if (!System.IO.File.Exists(path))
                throw new FileNotFoundException(path);

            await _blobStorage.AddFileAsync(blobId, $"{blobId}.pdf", System.IO.File.OpenRead(path), "application/pdf", context.Message.Bucket);

            await context.Publish<ConvertedToPdf>(new
            {
                Bucket = context.Message.Bucket,
                BlobId = blobId,
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                CorrelationId = context.Message.CorrelationId
            });
        }

        public async Task Consume(ConsumeContext<ExtractMeta> context)
        {
            var meta = new List<Property>()
            {
                new Property( "CreatedBy", "John Doe"),
                new Property("CreatedDateTime", DateTimeOffset.UtcNow)
            };

            await context.Publish<MetaExtracted>(new
            {
                Bucket = context.Message.Bucket,
                BlobId = context.Message.BlobId,
                Meta = meta,
                Id = context.Message.Id,
                UserId = context.Message.UserId,
                TimeStamp = DateTimeOffset.UtcNow,
                CorrelationId = context.Message.CorrelationId
            });
        }
    }
}
