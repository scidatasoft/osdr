using FluentAssertions;
using MassTransit;
using Sds.ChemicalExport.Domain.Commands;
using Sds.ChemicalExport.Domain.Events;
using Sds.MassTransit.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalExport.Tests
{
    public class ChemicalExportTests
        : IClassFixture<ExportTestFixture>
    {
        private readonly ExportTestFixture _fixture;
        private readonly string bucket = "test";
        public ChemicalExportTests(ExportTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task TestExportToCsv()
        {
            await _fixture.Harness.Start();

            var drugbank = Resource.DrugBank_10;
            var stream = new MemoryStream(drugbank);

            var id = NewId.NextGuid();
            var format = "csv";
            var blobId = await _fixture.BlobStorage.AddFileAsync("drugbank.sdf", stream, "", bucket);


            await _fixture.Harness.Bus.Publish<ExportFile>(new
            {
                Id = id,
                Format = format,
                BlobId = blobId,
                Bucket = bucket,
                UserId = _fixture.UserId,
                Properties = new List<string> { "Properties.Fields.INCHI", "Properties.Fields.DATABASE_NAME", "Properties.Fields.EXACT_MASS" },
                Map = new Dictionary<string, string> { { "Properties.Fields.INCHI", "InChI" } }
            });
            await _fixture.Harness.WaitWhileAllProcessed(TimeSpan.FromSeconds(15));
            await _fixture.Harness.Published.Any<ExportFinished>();
            var exportFinishedEvent = _fixture.Harness.Published.ToList().Select<ExportFinished>().FirstOrDefault();
            exportFinishedEvent.Should().NotBeNull();
            var exportedFile = await _fixture.BlobStorage.GetFileAsync(exportFinishedEvent.ExportBlobId, exportFinishedEvent.ExportBucket);
            var exportedStream = exportedFile.GetContentAsStream();

            using (var fileStream = File.Create("newCsvFile.csv"))
            {
                exportedStream.Seek(0, SeekOrigin.Begin);
                exportedStream.CopyTo(fileStream);
            }

            exportedFile.Info.Length.Should().NotBe(0);
        }

        [Fact]
        public async Task TestExportToSdf()
        {
            await _fixture.Harness.Start();

            var drugbank = Resource.DrugBank_10;
            var stream = new MemoryStream(drugbank);

            var id = NewId.NextGuid();
            var format = "sdf";
            var blobId = await _fixture.BlobStorage.AddFileAsync("drugbank.sdf", stream, "", bucket);

            await _fixture.Harness.Bus.Publish<ExportFile>(new
            {
                Id = id,
                Format = format,
                BlobId = blobId,
                Bucket = bucket,
                UserId = _fixture.UserId,
                Properties = new List<string> { "Properties.Fields.InChI", "Properties.Fileds.SMILES", "Properties.ChemicalProperties.MOLECULAR_FORMULA" },
                Map = new Dictionary<string, string> { { "Properties.Fields.INCHI", "InChI" } }
            });

            await _fixture.Harness.Published.Any<ExportFinished>();
            
            var exportFinishedEvent = _fixture.Harness.Published.ToList().Select<ExportFinished>().FirstOrDefault();
            var exportedFile = await _fixture.BlobStorage.GetFileAsync(exportFinishedEvent.ExportBlobId, exportFinishedEvent.ExportBucket);
            var exportedStream = exportedFile.GetContentAsStream();

            using (var fileStream = File.Create("newSdfFile.sdf"))
            {
                exportedStream.Seek(0, SeekOrigin.Begin);
                exportedStream.CopyTo(fileStream);
            }

            exportedFile.Info.Length.Should().NotBe(0);
        }
    }
}
