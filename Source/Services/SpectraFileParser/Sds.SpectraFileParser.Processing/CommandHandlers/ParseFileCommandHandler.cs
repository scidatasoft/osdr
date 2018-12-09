using MassTransit;
using Sds.Domain;
using Sds.FileParser;
using Sds.JcampParser;
using Sds.SpectraFileParser.Domain.Commands;
using Sds.SpectraFileParser.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.SpectraFileParser.Processing.CommandHandlers
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
            try
            {
                var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);
                var fields = new List<string>();

                IEnumerable<Record> records = null;

                switch (Path.GetExtension(blob.Info.FileName).ToLower())
                {
                    case ".dx":
                    case ".jdx":
                        records = new JcampReader(blob.GetContentAsStream());
                        break;
                    default:
                        await context.Publish<FileParseFailed>(new
                        {
                            Id = context.Message.Id,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow,
                            CorrelationId = context.Message.CorrelationId,
                            Message = $"Cannot find file parser for {blob.Info.FileName}"
                        });
                        break;
                }

                long totalRecords = 0;
                string bucket = context.Message.Bucket;

                foreach (var record in records)
                {
                    var blobId = NewId.NextGuid();

                    fields.AddRange(record.Properties?.Select(p => p.Name).Where(n => !fields.Contains(n)).ToList());

                    await blobStorage.AddFileAsync(blobId, blobId + Path.GetExtension(blob.Info.FileName).ToLower(), new MemoryStream(Encoding.UTF8.GetBytes(record.Data)), "chemical/x-jcamp-dx", bucket);

                    await context.Publish<RecordParsed>(new
                    {
                        Id = NewId.NextGuid(),
                        FileId = context.Message.Id,
                        Bucket = bucket,
                        BlobId = blobId,
                        Index = record.Index,
                        Fields = record.Properties?.Select(p => new Field(p.Name, p.Value)),
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });

                    totalRecords++;
                }

                await context.Publish<FileParsed>(new
                {
                    Id = context.Message.Id,
                    TotalRecords = totalRecords,
                    Fields = fields,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId
                });
            }
            catch (Exception e)
            {
                await context.Publish<FileParseFailed>(new
                {
                    Id = context.Message.Id,
                    UserId = context.Message.UserId,
                    TimeStamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.Message.CorrelationId,
                    Message = $"Cannot parse spectra file from bucket {context.Message.Bucket} with Id {context.Message.BlobId}. Error: {e.Message}"
                });
            }
        }
    }
}
