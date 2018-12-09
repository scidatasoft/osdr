using MassTransit;
using Sds.Domain;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using Sds.OfficeProcessor.Processing.MetaExtractors;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.OfficeProcessor.Processing.CommandHandlers
{
    public class ExtractMetaCommandHandler : IConsumer<ExtractMeta>
    {
        private readonly IBlobStorage blobStorage;

        public ExtractMetaCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<ExtractMeta> context)
        {
            try
            {
                var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);
                IEnumerable<Property> meta = null;
                IMetaExtractor extractor = null;

                var tempFilePath = Path.GetTempFileName();

                using (var fileStream = File.Create(tempFilePath))
                {
                    blob.GetContentAsStream().CopyTo(fileStream);
                }

                using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None,
                           4096, FileOptions.RandomAccess | FileOptions.DeleteOnClose))
                {
                    switch (Path.GetExtension(blob.Info.FileName).ToLower())
                    {
                        case ".doc":
                        case ".docx":
                        case ".odt":
                            extractor = new DocMetaExtractor();
                            meta = extractor.GetMeta(fs);
                            break;

                        case ".xls":
                        case ".xlsx":
                        case ".ods":
                            extractor = new ExcelMetaExtractor();
                            meta = extractor.GetMeta(fs);
                            break;

                        case ".ppt":
                        case ".pptx":
                        case ".odp":
                            extractor = new PresentationMetaExtractor();
                            meta = extractor.GetMeta(fs);
                            break;

                        default:
                            await context.Publish<MetaExtractionFailed>(new
                            {
                                Id = context.Message.Id,
                                UserId = context.Message.UserId,
                                TimeStamp = DateTimeOffset.UtcNow,
                                CorrelationId = context.Message.CorrelationId,
                                Message = $"Cannot find file converter for {blob.Info.FileName}"
                            });
                            break;
                    }

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
            catch(Exception e)
            {
                await context.Publish<MetaExtractionFailed>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId,
                    Message = $"Cannot convert file to pdf from bucket {context.Message.Bucket} with Id {context.Message.BlobId}. Error: {e.Message}"
                });
            }
        }
    }
}
