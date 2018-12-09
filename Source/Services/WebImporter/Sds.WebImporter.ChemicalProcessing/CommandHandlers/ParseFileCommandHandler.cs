using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.Domain;
using Sds.FileParser;
using Sds.Storage.Blob.Core;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.WebImporter.ChemicalProcessing.CommandHandlers
{
    public class ParseFileCommandHandler : IConsumer<ParseWebPage>
    {
        private readonly IBlobStorage blobStorage;

        public ParseFileCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<ParseWebPage> context)
        {
            var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            var importedFrom = blob.Info.Metadata["ImportedFrom"];

            IEnumerable<Record> records = null;

            switch (importedFrom)
            {
                case "wikipedia.org":
                    records = new WikipediaReader(blob.GetContentAsStream());
                    break;
                default:
                    //
                    break;
            }

            var bucket = context.Message.Bucket;
            long totalRecords = 0;
            List<string> fields = new List<string>();

            foreach (var record in records)
            {
                var blobId = Guid.NewGuid();

                var extension = "";
                var mimetype = "";

                switch (record.Type)
                {
                    case RecordType.Chemical:
                        extension = "mol";
                        mimetype = "chemical/x-mdl-molfile";
                        break;
                    case RecordType.Crystal:
                        extension = "cif";
                        mimetype = "chemical/x-cif";
                        break;
                    case RecordType.Reaction:
                        extension = "rxn";
                        mimetype = "chemical/x-mdl-rxn";
                        break;
                    case RecordType.Spectrum:
                        extension = "jdx";
                        mimetype = "chemical/x-jcamp-dx";
                        break;
                    default:
                        extension = "txt";
                        mimetype = "text/plain";
                        break;
                }

                await blobStorage.AddFileAsync(blobId, $"{blobId}.{extension}", new MemoryStream(Encoding.UTF8.GetBytes(record.Data == null ? "" : record.Data)), mimetype, bucket);

                fields.AddRange(record.Properties.Select(p => p.Name).Where(n => !fields.Contains(n)).ToList());

                await context.Publish<RecordParsed>(new
                {
                    Id = NewId.NextGuid(),
                    CorrelationId = context.Message.CorrelationId,
                    UserId = context.Message.UserId,
                    FileId = context.Message.Id,
                    Bucket = bucket,
                    BlobId = blobId,
                    Index = record.Index,
                    Fields = record.Properties?.Select(p => new Field(p.Name, p.Value))
                });

                totalRecords++;
            }

            await context.Publish<WebPageParsed>(new
            {
                Id = context.Message.Id,
                CorrelationId = context.Message.CorrelationId,
                UserId = context.Message.UserId,
                TotalRecords = totalRecords,
                Fields = fields
            });
        }
    }
}
