using MassTransit;
using Sds.Storage.Blob.Core;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using Sds.WebImporter.PdfProcessing.Convert;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace Sds.WebImporter.PdfProcessing.CommandHandlers
{
    public class GeneratePdfFromHtmlCommandHandler : IConsumer<GeneratePdfFromHtml>
    {
        private readonly IBlobStorage blobStorage;

        public GeneratePdfFromHtmlCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<GeneratePdfFromHtml> context)
        {
            var message = context.Message;
            try
            {


                WebClient client = new WebClient();
                Stream stream = client.OpenRead(message.Url);
                string title = "no-title";
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var content = reader.ReadToEnd();
                    title = Regex.Match(content, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                    stream.Flush();
                }

                var tempFileName = Path.GetTempFileName();

                var url = context.Message.Url;

                var pdfBytes = HtmlToPdf.GetPdfAsByteArray(message.Url);

                var dataStream = new MemoryStream(pdfBytes);

                dataStream.Seek(0, SeekOrigin.Begin);

                var blobId = Guid.NewGuid();

                await blobStorage.AddFileAsync(blobId, $"{blobId}.pdf", dataStream, "application/pdf", context.Message.Bucket);

                var blobInfo = await blobStorage.GetFileInfo(blobId, context.Message.Bucket);

                await context.Publish<PdfGenerated>(new
                {
                    Id = NewId.NextGuid(),
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    Bucket = context.Message.Bucket,
                    Title = title,
                    BlobId = blobId,
                    PageId = context.Message.Id,
                    Lenght = blobInfo.Length,
                    Md5 = blobInfo.MD5
                });
            }
            catch (Exception e)
            {

                await context.Publish<PdfGenerationFailed>(new
                {
                    Id = NewId.NextGuid(),
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    Message = $"Can not get pdf from url {context.Message.Url}. Details: {e.Message}"
                });
            }
        }

    }
}
