using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using System;
using System.Threading.Tasks;

namespace Sds.Osdr.IntegrationTests.Moq
{
    public class ChemicalFileParser : IConsumer<ParseFile>
    {
        private readonly IBlobStorage _blobStorage;

        public ChemicalFileParser(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ParseFile> context)
        {
            var blob = await _blobStorage.GetFileAsync(context.Message.BlobId, context.Message.Bucket);

            switch (blob.Info.FileName.ToLower())
            {
				case "ringcount_0.mol":
					{
						var blobId = Guid.NewGuid();
						var startTime = DateTimeOffset.UtcNow;
                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = 1,
							ParsedRecords = 0,
							FailedRecords = 1,
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = startTime
                        });

                        await context.Publish<RecordParseFailed>(new
						{
							Id = NewId.NextGuid(),
							FileId = context.Message.Id,
							Index = 0,
							Message = "molfile loader: ring bond count is allowed only for queries",
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
							TimeStamp = startTime
                        });
					}
					break;
                case "aspirin.mol":
                    {
                        var blobId = Guid.NewGuid();
                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = 0,
                            Fields = new Field[] {
                                new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                            },
                            Bucket = context.Message.Bucket,
                            BlobId = blobId,
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = 1,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" },
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        break;
                    }
				case "test_solubility.sdf":
					{
						await context.Publish<RecordParseFailed>(new
						{
							Id = NewId.NextGuid(),
							FileId = context.Message.Id,
							Index = 0,
							Message = "sdffile loader: could not process file",
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
							TimeStamp = DateTimeOffset.UtcNow
                        });

                        var blobId = Guid.NewGuid();
                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = 1,
                            Fields = new Field[] {
                                new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                            },
                            Bucket = context.Message.Bucket,
                            BlobId = blobId,
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = 2,
							ParsedRecords = 1,
							FailedRecords = 1,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" },
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
					}
					break;
                case "invalid_sdf_with_20_records_where_first_and_second_are_invalid.sdf":
                    {
                        await context.Publish<RecordParseFailed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = 0,
                            Message = "sdffile loader: could not process file",
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        await context.Publish<RecordParseFailed>(new
                        {
                            Id = NewId.NextGuid(),
                            FileId = context.Message.Id,
                            Index = 1,
                            Message = "sdffile loader: could not process file",
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        for (var i = 2; i < 20; i++)
                        {
                            var blobId = Guid.NewGuid();
                            await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                            await context.Publish<RecordParsed>(new
                            {
                                Id = NewId.NextGuid(),
                                FileId = context.Message.Id,
                                Index = i,
                                Fields = new Field[] {
                                    new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                    new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                    new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                                },
                                Bucket = context.Message.Bucket,
                                BlobId = blobId,
                                CorrelationId = context.Message.CorrelationId,
                                UserId = context.Message.UserId,
                                TimeStamp = DateTimeOffset.UtcNow
                            });
                        }

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = 20,
                            ParsedRecords = 19,
                            FailedRecords = 1,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" },
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                    }
                    break;
                case "drugbank_10_records.sdf":
                case "combined lysomotrophic.sdf":
                    {
                        var totalRecords = 2;

                        for (var i = 0; i < totalRecords; i++)
                        {
                            var blobId = Guid.NewGuid();
                            await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                            await context.Publish<RecordParsed>(new
                            {
                                Id = NewId.NextGuid(),
                                FileId = context.Message.Id,
                                Index = i,
                                Fields = new Field[] {
                                    new Field("StdInChI", $"StdInChI-{i}"),
                                    new Field("StdInChIKey", $"StdInChIKey-{i}"),
                                    new Field("SMILES", $"SMILES-{i}")
                                },
                                Bucket = context.Message.Bucket,
                                BlobId = blobId,
                                CorrelationId = context.Message.CorrelationId,
                                UserId = context.Message.UserId,
                                TimeStamp = DateTimeOffset.UtcNow
                            });
                        }

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = totalRecords,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" },
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });

                        break;
                    }
                case "125_11mos.cdx":
                    {
                        var totalRecords = 3;

                        for (var i = 0; i < totalRecords; i++)
                        {
                            var blobId = Guid.NewGuid();
                            await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", blob.GetContentAsStream(), "chemical/x-mdl-molfile", context.Message.Bucket);

                            await context.Publish<RecordParsed>(new
                            {
                                Id = NewId.NextGuid(),
                                FileId = context.Message.Id,
                                Index = i,
                                Fields = new Field[] {},
                                Bucket = context.Message.Bucket,
                                BlobId = blobId,
                                CorrelationId = context.Message.CorrelationId,
                                UserId = context.Message.UserId,
                                TimeStamp = DateTimeOffset.UtcNow
                            });
                        }

                        await context.Publish<FileParsed>(new
                        {
                            Id = context.Message.Id,
                            TotalRecords = totalRecords,
                            Fields = new string[] {},
                            CorrelationId = context.Message.CorrelationId,
                            UserId = context.Message.UserId,
                            TimeStamp = DateTimeOffset.UtcNow
                        });
                        break;
                    }
                default:
                    await context.Publish<FileParseFailed>(new
                    {
                        Id = context.Message.Id,
                        Message = $"Cannot parse chemical file {blob.Info.FileName}. Format is not supported.",
                        CorrelationId = context.Message.CorrelationId,
                        UserId = context.Message.UserId,
                        TimeStamp = DateTimeOffset.UtcNow
                    });
                    break;
            }
        }
    }
}
