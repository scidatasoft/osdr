using MassTransit;
using Sds.Storage.Blob.Core;
using Sds.WebImporter.Domain;
using Sds.WebImporter.Domain.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class ProcessWebPageCommandHandler : IConsumer<ProcessWebPage>
    {
        private readonly IBlobStorage blobStorage;

        public ProcessWebPageCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<ProcessWebPage> context)
        {
            var message = context.Message;
            try
            {
                string content = "";
                var meta = new Dictionary<string, object>();
                
                if (message.Url.ToLower().Contains("chemspider"))
                {
                    var cs = new Chemspider(new List<string> { message.Url });
                    content = cs.Content;
                    meta = cs.Meta;
                }

                if (message.Url.Contains("wikipedia"))
                {
                    try
                    {
                        var wiki = new Wikipedia(new List<string> { message.Url });
                        content = wiki.Content;
                        meta = wiki.Meta;
                    }
                    catch(Exception e)
                    {
                        //
                    }
                }


                if (content != "" && content != "{}")
                {
                    Guid blobId = Guid.NewGuid();
                    string fileName = $"{blobId}.json";
                    await blobStorage.AddFileAsync(blobId, fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)), "application/json", message.Bucket, meta);

                    await context.Publish<WebPageProcessed>(new
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        UserId = message.UserId,
                        BlobId = blobId,
                        Bucket = message.Bucket
                    });
                }
                else
                {
                    await context.Publish<WebPageProcessed>(new
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        UserId = message.UserId
                    });
                }
            }
            catch (Exception e)
            {
                await context.Publish<WebPageProcessFailed>(new
                {
                    Id = message.Id,
                    CorrelationId = message.CorrelationId,
                    UserId = message.UserId,
                    Message = e.Message
                });
            }
        }
    }
}
