using MassTransit;
using Sds.Domain;
using Sds.SpectraFileParser.Domain.Commands;
using Sds.SpectraFileParser.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class SpectraFileParser : IConsumer<ParseFile>
    {
        private readonly IBlobStorage _blobStorage;

        public SpectraFileParser(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ParseFile> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            switch (blob.Info.FileName.ToLower())
            {
                case "13csample.jdx":
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId,
                        Message = $"Cannot parse spectra file {blob.Info.FileName}."
                    });
                    break;

                case "2-methyl-1-propanol.jdx":
                    var blobId = Guid.NewGuid();
                    await _blobStorage.AddFileAsync(blobId, $"{blobId}.jdx", blob.GetContentAsStream(), "chemical/x-jcamp-dx", context.Message.Bucket);

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
                default:
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId,
                        Message = $"Cannot parse spectra file {blob.Info.FileName}. Format is not supported."
                    });
                    break;
            }
        }
    }
}
