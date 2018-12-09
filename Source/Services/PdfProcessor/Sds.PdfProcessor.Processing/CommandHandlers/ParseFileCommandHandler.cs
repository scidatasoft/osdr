using CQRSlite.Commands;
using CQRSlite.Events;
using Sds.PdfProcessor.Domain.Commands;
using Sds.PdfProcessor.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sds.PdfProcessor.Processing.CommandHandlers
{
    public class ParseFileCommandHandler : ICancellableCommandHandler<ParseFile>
    {
        private readonly IBlobStorage blobStorage;
        private readonly IEventPublisher eventPublisher;

        public ParseFileCommandHandler(IBlobStorage blobStorage, IEventPublisher eventPublisher)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
            this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task Handle(ParseFile message, CancellationToken token)
        {
            try
            {
                var blob = await blobStorage.GetFileAsync(message.BlobId, message.Bucket);

                string txtData = null;

                Dictionary<string, byte[]> images = null;

                //tables = null;


                switch (Path.GetExtension(blob.Info.FileName).ToLower())
                {
                    case ".pdf":
                        
                        if(((OperationType)message.ByteTypes & OperationType.Text) != 0)
                        {
                            txtData = PdfImporter.GetText(blob.GetContentAsStream(), blob.Info.FileName);
                        }

                        if (((OperationType)message.ByteTypes & OperationType.Images) != 0)
                        {
                            images = PdfImporter.GetImagesAsBytes(blob.GetContentAsStream(), blob.Info.FileName);
                        }

                        if (((OperationType)message.ByteTypes & OperationType.Tables) != 0)
                        {
                            //tables
                        }
                        break;
                    default:
                        await eventPublisher.Publish(new FileParseFailed(message.Id, message.CorrelationId, message.UserId, $"Cannot find file parser for {blob.Info.FileName}"));
                        break;
                }

                string bucket = message.Bucket;

                if(txtData != null)
                {
                    var txtFileId = Guid.NewGuid();

                    await blobStorage.AddFileAsync(txtFileId, $"Text from {blob.Info.FileName}.txt", new MemoryStream(Encoding.UTF8.GetBytes(txtData)), "text/plain", bucket);

                    await eventPublisher.Publish(new TextExported(message.Id, message.CorrelationId, message.UserId, txtFileId));
                }

                if(images != null && images.Count != 0)
                {
                    var imgCount = 0;

                    foreach (var img in images)
                    {
                        if (img.Value!= null)
                        {
                            var imgId = Guid.NewGuid();

                            await blobStorage.AddFileAsync(imgId, $"{img.Key}", new MemoryStream(img.Value), "image/jpeg", bucket);

                            imgCount++;

                            await eventPublisher.Publish(new ImageExported(imgId, message.CorrelationId, message.UserId, message.BlobId, imgCount));
                        }
                    }
                }

                await eventPublisher.Publish(new FileParsed(message.Id, message.CorrelationId, message.UserId, message.ByteTypes));
            }
            catch(Exception e)
            {
                await eventPublisher.Publish(new FileParseFailed(message.Id, message.CorrelationId, message.UserId, $"Cannot parse pdf file from bucket {message.Bucket} with Id {message.BlobId}. Error: {e.Message}"));
            }
        }
    }
}
