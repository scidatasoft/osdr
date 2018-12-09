using MassTransit;
using Sds.Domain;
using Sds.ReactionFileParser.Domain.Commands;
using Sds.ReactionFileParser.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class ReactionFileParser : IConsumer<ParseFile>
    {
        private readonly IBlobStorage _blobStorage;

        public ReactionFileParser(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ParseFile> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            switch (blob.Info.FileName.ToLower())
            {
                case "10001.rxn":
                    var blobId = Guid.NewGuid();
                    await _blobStorage.AddFileAsync(blobId, $"{blobId}.rxn", blob.GetContentAsStream(), "chemical/x-mdl-rxnfile", context.Message.Bucket);

                    var fields = new Field[] {
                        new Field("Field1", "Value1"),
                        new Field("Field2", "Value2")
                    };

                    await context.Publish<RecordParsed>(new
                    {
                        Id = NewId.NextGuid(),
                        FileId = context.Message.Id,
                        Bucket = context.Message.Bucket,
                        BlobId = blobId,
                        Index = 0,
                        Fields = fields,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });

                    await context.Publish<FileParsed>(new
                    {
                        Id = context.Message.Id,
                        TotalRecords = 1,
                        Fields = fields.Select(f => f.Name),
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId
                    });
                    break;
                case "empty.rxn":
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId,
                        Message = $"Cannot parse chemical file {blob.Info.FileName}. Format is not supported."
                    });
                    break;
                default:
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId,
                        Message = $"Cannot parse reaction file {blob.Info.FileName}. Format is not supported."
                    });
                    break;
            }
        }
    }
}
