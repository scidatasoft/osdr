using MassTransit;
using Sds.ChemicalExport.Domain;
using Sds.ChemicalExport.Domain.Commands;
using Sds.ChemicalExport.Domain.Events;
using Sds.ChemicalExport.Domain.Interfaces;
using Sds.ChemicalExport.Domain.Models;
using Sds.ChemicalExport.Processing.Workers;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sds.ChemicalExport.Processing.CommandHandlers
{
    public class ExportFileCommandHandler: IConsumer<ExportFile>
    {
        private readonly IBlobStorage blobStorage;
        private readonly IRecordsProvider provider;

        public ExportFileCommandHandler(IBlobStorage blobStorage
            , IRecordsProvider provider
            )
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task Consume(ConsumeContext<ExportFile> context)
        {
            try
            {
                var blobInfo = await blobStorage.GetFileInfo(context.Message.BlobId, context.Message.Bucket);

                if (blobInfo == null)
                {
                    await context.Publish<ExportFailed>(new
                    {
                        Id = context.Message.Id,
                        FileId = context.Message.BlobId,
                        Bucket = context.Message.Bucket,
                        Format = context.Message.Format,
                        Message = $"Cannot find blob with id {context.Message.BlobId} in bucket {context.Message.Bucket}."
                    });
                    return;
                }

                IChemicalExport exporter = null;

                switch (context.Message.Format.ToLower())
                {
                    case "sdf":
                        exporter = new SdfExport();
                        break;
                    case "csv":
                        exporter = new CsvExport();
                        break;
                    case "spl":
                        exporter = new SplExport();
                        break;
                    default:
                        await context.Publish<ExportFailed>(new
                        {
                            Id = context.Message.Id,
                            FileId = context.Message.BlobId,
                            Bucket = context.Message.Bucket,
                            Format = context.Message.Format,
                            Message = $"Queried export format is not supported.",
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                        return;
                }

                var fileName = $"{Path.GetFileNameWithoutExtension(blobInfo.FileName)}-" +
                    //$"{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ")}" +
                    $".{context.Message.Format.ToLower()}";

                if (exporter != null)
                {
                    var blobId = NewId.NextGuid();
                    using (var stream = await blobStorage.OpenUploadStreamAsync(blobId, fileName, MimeType.GetFileMIME(context.Message.Format), context.Message.Bucket))
                    {
                        var recordsEnumerator = await provider.GetRecords(context.Message.BlobId, context.Message.Bucket);

                        await exporter.Export(recordsEnumerator, stream, context.Message.Properties, context.Message.Map);

                        await stream.FlushAsync();

                        await context.Publish<ExportFinished>(new
                        {
                            Id = context.Message.Id,
                            SourceBlobId = context.Message.BlobId,
                            ExportBlobId = blobId,
                            Filename = fileName,
                            Format = context.Message.Format,
                            ExportBucket = context.Message.Bucket,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch (Exception e)
            {
                await context.Publish<ExportFailed>(new
                {
                    Id = context.Message.Id,
                    FileId = context.Message.BlobId,
                    Bucket = context.Message.Bucket,
                    Format = context.Message.Format,
                    Message = $"File can not be exported. Details: {e.Message}",
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
