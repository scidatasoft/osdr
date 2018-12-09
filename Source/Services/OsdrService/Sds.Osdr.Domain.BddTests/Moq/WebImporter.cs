using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.Domain;
using Sds.Storage.Blob.Core;
using Sds.WebImporter.Domain;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sds.Osdr.Domain.IntegrationTests.Moq
{
    public class WebImporter : IConsumer<ProcessWebPage>,
                                IConsumer<ParseWebPage>,
                                IConsumer<GeneratePdfFromHtml>
    {
        private readonly IBlobStorage _blobStorage;

        public WebImporter(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage)); ;
        }

        public async Task Consume(ConsumeContext<ProcessWebPage> context)
        {
            var message = context.Message;
            switch (message.Url)
            {
                case "https://en.wikipedia.org/wiki/Aspirin":
                    {

                        var blobId = NewId.NextGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "wiki.json");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, "wiki.mol", System.IO.File.OpenRead(path), "application/json", message.Bucket);

                        var blobInfo = await _blobStorage.GetFileInfo(blobId, message.Bucket);

                        await context.Publish<WebPageProcessed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            BlobId = (Guid)blobId,
                            Bucket = message.Bucket
                        });
                        break;
                    }
                case "http://www.chemspider.com/Chemical-Structure.2157.html?rid=d8424976-d183-431d-9d19-b663a5c4b1df":
                    {
                        var blobId = NewId.NextGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "chemspider.json");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, "chemspider.mol", System.IO.File.OpenRead(path), "application/json", message.Bucket);

                        var blobInfo = await _blobStorage.GetFileInfo(blobId, message.Bucket);

                        await context.Publish<WebPageProcessed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            BlobId = (Guid)blobId,
                            Bucket = message.Bucket
                        });
                        break;
                    }
                case "http://lifescience.opensource.epam.com/indigo/api/#loading-molecules-and-query-molecules":
                    {

                        var blobId = NewId.NextGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Generic.pdf");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, "Generic.pdf", System.IO.File.OpenRead(path), "application/json", message.Bucket);

                        var blobInfo = await _blobStorage.GetFileInfo(blobId, message.Bucket);

                        await context.Publish<WebPageProcessed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            BlobId = (Guid)blobId,
                            Bucket = message.Bucket
                        });
                        break;
                    }
                default:
                    {
                        await context.Publish<WebPageProcessFailed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            Message = $"Cannot import file. Format is not supported."
                        });
                        break;
                    }
            }
        }

        public async Task Consume(ConsumeContext<ParseWebPage> context)
        {
            var message = context.Message;
            var blob = await _blobStorage.GetFileAsync(message.BlobId, message.Bucket);

            switch (blob.Info.FileName.ToLower())
            {
                case "wiki.mol":
                    {
                        var blobId = NewId.NextGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "wikiAspirin.mol");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", System.IO.File.OpenRead(path), "chemical/x-mdl-molfile", message.Bucket);

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            FileId = message.Id,
                            Bucket = message.Bucket,
                            BlobId = blobId,
                            Index = 0,
                            Fields = new Field[]
                            {
                                    new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                    new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                    new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                            }
                        });

                        await context.Publish<WebPageParsed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            TotalRecords = 1,
                            UserId = message.UserId,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" }
                        });

                        break;
                    }

                case "chemspider.mol":
                    {
                        var blobId = Guid.NewGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "chemspider.mol");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", System.IO.File.OpenRead(path), "chemical/x-mdl-molfile", message.Bucket);

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            FileId = message.Id,
                            Bucket = message.Bucket,
                            BlobId = blobId,
                            Index = 0,
                            Fields = new Field[]
                            {
                                    new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                    new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                    new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                            }
                        });

                        blobId = Guid.NewGuid();

                        path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "chemspider2.mol");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, $"{blobId}.mol", System.IO.File.OpenRead(path), "chemical/x-mdl-molfile", message.Bucket);

                        await context.Publish<RecordParsed>(new
                        {
                            Id = NewId.NextGuid(),
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            FileId = message.Id,
                            Bucket = message.Bucket,
                            BlobId = blobId,
                            Index = 0,
                            Fields = new Field[]
                            {
                                    new Field("StdInChI", "InChI=1S/C9H8O4/c1-6(10)13-8-5-3-2-4-7(8)9(11)12/h2-5H,1H3,(H,11,12)"),
                                    new Field("StdInChIKey", "BSYNRYMUTXBXSQ-UHFFFAOYSA-N"),
                                    new Field("SMILES", "CC(OC1=C(C(=O)O)C=CC=C1)=O")
                            }
                        });

                        await context.Publish<WebPageParsed>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            TotalRecords = 2,
                            UserId = message.UserId,
                            Fields = new string[] { "StdInChI", "StdInChIKey", "SMILES" }
                        });
                        break;
                    }
            }
        }

        public async Task Consume(ConsumeContext<GeneratePdfFromHtml> context)
        {
            var message = context.Message;
            switch (message.Url)
            {
                case "http://lifescience.opensource.epam.com/indigo/api/#loading-molecules-and-query-molecules":
                    {

                        var blobId = Guid.NewGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "Generic.pdf");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, "Generic.pdf", System.IO.File.OpenRead(path), "application/pdf", message.Bucket);

                        var blobInfo = await _blobStorage.GetFileInfo(blobId, message.Bucket);

                        await context.Publish<PdfGenerated>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            Bucket = message.Bucket,
                            BlobId = blobId,
                            PageId = message.Id,
                            Lenght = blobInfo.Length,
                            Md5 = blobInfo.MD5
                        });

                        break;
                    }

                case "https://en.wikipedia.org/wiki/Aspirin":
                    {
                        var blobId = Guid.NewGuid();

                        var path = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "WikiAspirin.pdf");

                        if (!System.IO.File.Exists(path))
                            throw new FileNotFoundException(path);

                        await _blobStorage.AddFileAsync(blobId, "WikiAspirin.pdf", System.IO.File.OpenRead(path), "application/pdf", message.Bucket);

                        var blobInfo = await _blobStorage.GetFileInfo(blobId, message.Bucket);

                        await context.Publish<PdfGenerated>(new
                        {
                            Id = message.Id,
                            CorrelationId = message.CorrelationId,
                            UserId = message.UserId,
                            Bucket = message.Bucket,
                            BlobId = blobId,
                            PageId = message.Id,
                            Lenght = blobInfo.Length,
                            Md5 = blobInfo.MD5
                        });
                        break;
                    }
                default:
                    await context.Publish<PdfGenerationFailed>(new
                    {
                        Id = message.Id,
                        CorrelationId = message.CorrelationId,
                        UserId = message.UserId,
                        Message = $"Cannot import file. Format is not supported."
                    });
                    break;
            }
        }
    }
}

