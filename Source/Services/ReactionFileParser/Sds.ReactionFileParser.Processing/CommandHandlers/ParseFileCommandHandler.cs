using MassTransit;
using Sds.Domain;
using Sds.FileParser;
using Sds.ReactionFileParser.Domain.Commands;
using Sds.ReactionFileParser.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sds.ReactionFileParser.Processing.CommandHandlers
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
            long totalRecords = 0;
            try
            {
                var blob = await blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

                IEnumerable<Record> records = null;

                switch (Path.GetExtension(blob.Info.FileName).ToLower())
                {
                    case ".rdf":
                    case ".rxn":
                        records = new RdfParser.RdfParser(blob.GetContentAsStream());
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

                
                string bucket = context.Message.Bucket;
                List<string> fields = new List<string>();
                var e = records.GetEnumerator();

                while(e.MoveNext())
                {
                    totalRecords++;
                    try
                    {
                        var record = e.Current; 

                        var blobId = NewId.NextGuid();

                        fields.AddRange(record.Properties.Select(p => p.Name).Where(n => !fields.Contains(n)).ToList());

                        await blobStorage.AddFileAsync(blobId, $"{blobId}.rxn", new MemoryStream(Encoding.UTF8.GetBytes(record.Data)), "chemical/x-mdl-rxnfile", bucket);

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
                    }
                    catch (Exception ex)
                    {
                        await context.Publish<RecordParseFailed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow,
                            CorrelationId = context.Message.CorrelationId,
                            Message = $"Cannot parse reaction record #{totalRecords} from file {context.Message.Id}. Error: {ex.Message}"
                        });
                    }

                    //  temporary limitation: we don't want to process more than 100 records inside any file
                    if (totalRecords >= 100)
                        break;
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
                    Message = $"Cannot parse reaction file from bucket {context.Message.Bucket} with Id {context.Message.Id}. Error: {e.Message}"
                });
            }
        }
    }
}
