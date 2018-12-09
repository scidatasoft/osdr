using MassTransit;
using Sds.CrystalFileParser.Domain.Commands;
using Sds.CrystalFileParser.Domain.Events;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class CrystalFileParser : IConsumer<ParseFile>
    {
        private readonly IBlobStorage _blobStorage;

        public CrystalFileParser(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ParseFile> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);
            var blobId = Guid.NewGuid();
            var fields = new Field[]
            {
                new Field("Field1", "Value1"),
                new Field("Field2", "Value2")
            };

            switch (blob.Info.FileName.ToLower())
            {
                case "invalidimagegeneration.cif":
                    await _blobStorage.AddFileAsync(blobId, blob.Info.FileName, blob.GetContentAsStream(), "chemical/x-cif", context.Message.Bucket);
                    
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
                case "1100110.cif":
                    await _blobStorage.AddFileAsync(blobId, $"{blobId}.cif", blob.GetContentAsStream(), "chemical/x-cif", context.Message.Bucket);

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
                case "1100110_modified.cif":
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow,
                        CorrelationId = context.Message.CorrelationId,
                        Message = $"Cannot parse spectra file {blob.Info.FileName}."
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
