using MassTransit;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using Sds.OfficeProcessor.Processing.Converters;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.OfficeProcessor.Processing.CommandHandlers
{
    public class ConvertToPdfCommandHandler : IConsumer<ConvertToPdf>
    {
        private readonly IBlobStorage blobStorage;

        public ConvertToPdfCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<ConvertToPdf> context)
        {
            try
            {
                var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);
                Stream data = null;
                IConvert converter = null;

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
                            converter = new DocToPdf();
                            data = converter.Convert(fs);
                            break;

                        case ".xls":
                        case ".xlsx":
                        case ".ods":
                            converter = new XlsToPdf();
                            data = converter.Convert(fs);
                            break;

                        case ".ppt":
                        case ".pptx":
                        case ".odp":
                            converter = new PptToPdf();
                            data = converter.Convert(fs);
                            break;

                        default:
                            await context.Publish<ConvertToPdfFailed>(new
                            {
                                Id = context.Message.Id,
                                UserId = context.Message.UserId,
                                TimeStamp = DateTimeOffset.UtcNow,
                                CorrelationId = context.Message.CorrelationId,
                                Message = $"Cannot find file converter for {blob.Info.FileName}"
                            });
                            break;
                    }

                    string bucket = context.Message.Bucket;

                    if (data != null)
                    {
                        var blobId = Guid.NewGuid();

                        data.Seek(0, SeekOrigin.Begin);

                        await blobStorage.AddFileAsync(blobId, $"{blobId}.pdf", data, "application/pdf", bucket);

                        await context.Publish<ConvertedToPdf>(new
                        {
                            Bucket = bucket,
                            BlobId = blobId,
                            Id = context.Message.Id,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow,
                            CorrelationId = context.Message.CorrelationId
                        });
                    }
                }
            }
            catch(Exception e)
            {
                await context.Publish<ConvertToPdfFailed>(new
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
