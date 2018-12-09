using MassTransit;
using Sds.Imaging.Domain.Commands;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Rasterizers;
using Sds.Storage.Blob.Core;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Imaging.Processing.CommandHandlers
{
    public class ImagingCommandHandler : IConsumer<GenerateImage>
    {
        private readonly IBlobStorage _blobStorage;

        public ImagingCommandHandler(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<GenerateImage> context)
        {
            try
            {
                var blobInfo = await _blobStorage.GetFileInfo(context.Message.BlobId, context.Message.Bucket);

                string extension = Path.GetExtension(blobInfo.FileName);

                if (!FileRasterizer.Supports(extension))
                    throw new InvalidDataException($"Unsupported file type {extension}");

                using (var stream = new MemoryStream())
                {
                    await _blobStorage.DownloadFileToStreamAsync(context.Message.BlobId, stream, context.Message.Bucket);
                    stream.Position = 0;

                    byte[] imageBytes;

                    if (context.Message.Image.Format.ToLower() != "svg")
                    {
                        var format = context.Message.Image.Format.ParseImageFormat();
                        var rasterizer = new FileRasterizer();

                        Log.Information($"Rasterizing source '{context.Message.BlobId}'");

                        var image = rasterizer.Rasterize(stream, extension);
                        image = image.Scale(context.Message.Image.Width, context.Message.Image.Height);
                        imageBytes = image.Convert(format);

                    }
                    else
                    {
                        string data = System.Text.Encoding.ASCII.GetString(stream.ToArray());
                        switch (extension.ToLower())
                        {
                            case ".mol":
                                imageBytes = new IndigoAdapter().Mol2Image(data, context.Message.Image.Format, context.Message.Image.Width, context.Message.Image.Height);
                                break;

                            case ".rxn":
                                imageBytes = new IndigoAdapter().Rxn2Image(data, context.Message.Image.Format, context.Message.Image.Width, context.Message.Image.Height);
                                break;

                            default:
                                throw new InvalidDataException($"Unsupported file type {extension} for {context.Message.Image.Format} generation");
                        }
                    }

                    Log.Information($"Saving image file {context.Message.Image.Id} as {context.Message.Image.Format}");

                    await _blobStorage.AddFileAsync(
                        id: context.Message.Image.Id,
                        fileName: $"{blobInfo.FileName}.{$"{context.Message.Image.Format}".ToLower()}",
                        source: imageBytes,
                        contentType: $"{context.Message.Image.Format}".GetMimeType(),
                        bucketName: context.Message.Bucket,
                        metadata: new Dictionary<string, object> { { "SourceId", context.Message.BlobId } }
                    );

                    Log.Information($"Image file {context.Message.Image.Id} as {context.Message.Image.Format} saved.");
                }

                context.Message.Image.MimeType = context.Message.Image.Format.GetMimeType();

                await context.Publish<ImageGenerated>(new
                {
                    Id = context.Message.Id,
                    Bucket = context.Message.Bucket,
                    BlobId = context.Message.BlobId,
                    Image = context.Message.Image,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception e)
            {
                context.Message.Image.Exception = e.Message;

                await context.Publish<ImageGenerationFailed>(new
                {
                    Id = context.Message.Id,
                    Image = context.Message.Image,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
