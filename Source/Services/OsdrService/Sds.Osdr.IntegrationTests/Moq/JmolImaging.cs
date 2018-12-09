using MassTransit;
using Sds.Imaging.Domain.Commands.Jmol;
using Sds.Imaging.Domain.Events;
using Sds.Imaging.Domain.Models;
using Sds.Storage.Blob.Core;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class JmolImaging : IConsumer<GenerateImage>
    {
        private readonly IBlobStorage _blobStorage;

        public JmolImaging(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<GenerateImage> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            if (blob.Info.FileName.Equals("InvalidImageGeneration.cif"))
            {
                context.Message.Image.Exception = "Cannot generate image";

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
    }
}
