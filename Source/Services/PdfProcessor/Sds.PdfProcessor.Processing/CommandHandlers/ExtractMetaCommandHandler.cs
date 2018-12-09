using CQRSlite.Commands;
using CQRSlite.Events;
using PdfSharp.Pdf;
using Sds.PdfProcessor.Domain.Commands;
using Sds.PdfProcessor.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.PdfProcessor.Processing.CommandHandlers
{
    public class ExtractMetaCommandHandler : ICancellableCommandHandler<ExtractMeta>
    {
        private readonly IBlobStorage blobStorage;
        private readonly IEventPublisher eventPublisher;

        public ExtractMetaCommandHandler(IBlobStorage blobStorage, IEventPublisher eventPublisher)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task Handle(ExtractMeta message, CancellationToken token)
        {
            try
            {
                var blob = await blobStorage.GetFileAsync(message.BlobId, message.Bucket);

                PdfDocument doc = null;

                switch (Path.GetExtension(blob.Info.FileName).ToLower())
                {
                    case ".pdf":
                        doc = new PdfDocument(blob.GetContentAsStream());
                        break;

                    default:
                        await eventPublisher.Publish(new MetaExtractionFailed(message.Id, message.CorrelationId, message.UserId, $"Cannot find file parser for {blob.Info.FileName}"));
                        break;
                }

                if(doc != null)
                {
                    var extractor = new PdfMetaExtractor(doc);
                    var meta = extractor.Meta;

                    await eventPublisher.Publish(new MetaExtracted(message.Id, message.CorrelationId, message.UserId, meta, message.Bucket, message.BlobId));
                }
            }
            catch(Exception e)
            {
                await eventPublisher.Publish(new MetaExtractionFailed(message.Id, message.CorrelationId, message.UserId, $"Cannot parse pdf file from bucket {message.Bucket} with Id {message.BlobId}. Error: {e.Message}"));
            }
        }
    }
}
