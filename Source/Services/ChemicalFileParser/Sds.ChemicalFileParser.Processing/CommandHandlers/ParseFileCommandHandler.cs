using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.Domain;
using Sds.FileParser;
using Sds.SdfParser;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.ChemicalFileParser.Processing.CommandHandlers
{
    public class ParseFileCommandHandler : IConsumer<ParseFile>
    {
        private readonly IBlobStorage blobStorage;

        public ParseFileCommandHandler(IBlobStorage blobStorage)
        {
            this.blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        }

        public async Task Consume(ConsumeContext<ParseFile> context)
        {
            var failedRecords = 0;
            var parsedRecords = 0;

            try
            {
                var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

                if (blob == null)
                {
                    throw new FileNotFoundException($"Blob with Id {context.Message.BlobId} not found in bucket {context.Message.Bucket}");
                }

                IEnumerable<Record> records = null;

                switch (Path.GetExtension(blob.Info.FileName).ToLower())
                {
                    case ".mol":
                    case ".sdf":
                        records = new SdfIndigoParser(blob.GetContentAsStream());
                        break;
                    case ".cdx":
                        records = new CdxParser.CdxParser(blob.GetContentAsStream());
                        break;
                    default:
                        await context.Publish<FileParseFailed>(new
                        {
                            Id = context.Message.Id,
                            Message = $"Cannot parse chemical file {blob.Info.FileName}. Format is not supported.",
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        return;
                }

                var bucket = context.Message.Bucket;
                var index = 0;
                List<string> fields = new List<string>();
                var enumerator = records.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    try
                    {
                        var record = enumerator.Current;

                        var blobId = Guid.NewGuid();

                        await blobStorage.AddFileAsync(blobId, $"{blobId}.mol", new MemoryStream(Encoding.UTF8.GetBytes(record.Data)), "chemical/x-mdl-molfile", bucket);

                        fields.AddRange(record.Properties.Select(p => p.Name).Where(n => !fields.Contains(n)).ToList());

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = index,
                            Fields = record.Properties?.Select(p => new Field(p.Name, p.Value)),
                            Bucket = bucket,
                            BlobId = blobId,
                            context.Message.CorrelationId,
                            context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        parsedRecords++;
                    }
                    catch (Exception ex)
                    {
                        await context.Publish<RecordParseFailed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = index,
                            ex.Message,
                            context.Message.CorrelationId,
                            context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                        failedRecords++;
                    }
                    index++;

                    //  temporary limitation: we don't want to process more than 100 records inside any file
                    if (index >= 100)
                        break;
                }

                await context.Publish<FileParsed>(new
                {
                    context.Message.Id,
                    FailedRecords = failedRecords,
                    ParsedRecords = parsedRecords,
                    TotalRecords = parsedRecords + failedRecords,
                    Fields = fields,
                    context.Message.CorrelationId,
                    context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                await context.Publish<FileParseFailed>(new
                {
                    context.Message.Id,
                    FailedRecords = failedRecords,
                    ParsedRecords = parsedRecords,
                    TotalRecords = parsedRecords + failedRecords,
                    ex.Message,
                    context.Message.CorrelationId,
                    context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}
