using MassTransit;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class Imaging : IConsumer<Sds.Imaging.Domain.Commands.GenerateImage>
    {
        private readonly IBlobStorage _blobStorage;

        public Imaging(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<Sds.Imaging.Domain.Commands.GenerateImage> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            if (Path.GetExtension(blob.Info.FileName).Equals(".jdx", StringComparison.CurrentCultureIgnoreCase) ||
                Path.GetExtension(blob.Info.FileName).Equals(".sav", StringComparison.CurrentCultureIgnoreCase))
            {
                var image = new Image()
                {
                    Id = context.Message.Image.Id,
                    Format = context.Message.Image.Format,
                    Height = context.Message.Image.Height,
                    Width = context.Message.Image.Width,
                    MimeType = "image/png",
                    Exception = $"Spectra formats are currently not supported"
                };

                //await _eventPublisher.Publish(new ImageGenerationFailed(context.Message.Id, image, context.Message.CorrelationId, context.Message.UserId));
                await context.Publish<ImageGenerationFailed>(new
                {
                    Id = context.Message.Id,
                    Image = context.Message.Image,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
            else if (Path.GetExtension(blob.Info.FileName).Equals(".cif", StringComparison.CurrentCultureIgnoreCase))
            {
                var image = new Image()
                {
                    Id = context.Message.Image.Id,
                    Format = context.Message.Image.Format,
                    Height = context.Message.Image.Height,
                    Width = context.Message.Image.Width,
                    MimeType = "image/png",
                    Exception = $"Crystal formats are currently not supported"
                };

                //await _eventPublisher.Publish(new ImageGenerationFailed(context.Message.Id, image, context.Message.CorrelationId, context.Message.UserId));
                await context.Publish<ImageGenerationFailed>(new
                {
                    Id = context.Message.Id,
                    Image = context.Message.Image,
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
            else if (context.Message.Image.Format.Equals("svg", StringComparison.CurrentCultureIgnoreCase))
            {
                if (Path.GetExtension(blob.Info.FileName).ToLower().Equals(".mol"))
                    await AddBlob(context.Message.Image.Id, context.Message.UserId, context.Message.Bucket, "Aspirin.mol.svg");

                var image = new Image()
                {
                    Id = context.Message.Image.Id,
                    Format = context.Message.Image.Format,
                    Height = context.Message.Image.Height,
                    Width = context.Message.Image.Width,
                    MimeType = "image/svg+xml"
                };

                //await _eventPublisher.Publish(new ImageGenerated(Guid.NewGuid(), context.Message.Bucket, Guid.NewGuid(), image, context.Message.CorrelationId, context.Message.UserId));
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
            else if (context.Message.Image.Format.Equals("png", StringComparison.CurrentCultureIgnoreCase))
            {
                await AddBlob(context.Message.Image.Id, context.Message.UserId, context.Message.Bucket, "Chemical-diagram.png");
                
                var image = new Image()
                {
                    Id = context.Message.Image.Id,
                    Format = context.Message.Image.Format,
                    Height = context.Message.Image.Height,
                    Width = context.Message.Image.Width,
                    MimeType = "image/png"
                };

                //await _eventPublisher.Publish(new ImageGenerated(Guid.NewGuid(), context.Message.Bucket, Guid.NewGuid(), image, context.Message.CorrelationId, context.Message.UserId));
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
        }

        public async Task AddBlob(Guid blobId, Guid userId, string bucket, string fileName, IDictionary<string, object> metadata = null)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName);

            if (File.Exists(path))
            {
                await _blobStorage.AddFileAsync(blobId, fileName, File.OpenRead(path), "application/octet-stream", bucket, metadata);
            }
        }
    }
}