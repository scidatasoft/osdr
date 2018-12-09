using FluentAssertions;
using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.MassTransit.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalFileParser.Tests
{
    public partial class ParseChemicalFileTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture fixture;

        public ParseChemicalFileTests(ParseFileTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task ParseDrugBankDatasetTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("DrugBank50.sdf", Resource.DrugBank_10, "chemical/x-mdl-sdfile", BUCKET);

                await fixture.Harness.Bus.Publish<ParseFile>(new
                {
                    Id = id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                });

                //await fixture.Harness.Published.Any<FileParsed>(m => m.Context.Message.CorrelationId == correlationId);
                var res = await fixture.Harness.WaitWhileAllProcessed();
                res.Should().BeTrue();

                var allEvents = fixture.Harness.Published.ToList();

                allEvents.Select<FileParsed>().Count().Should().Be(1);
                allEvents.Select<RecordParsed>().Count().Should().Be(10);

                var fileParsed = allEvents.Select<FileParsed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    TotalRecords = 10,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
                fileParsed.Fields.Count().Should().Be(41);

                var recordParsed = allEvents.Select<RecordParsed>().FirstOrDefault();
                recordParsed.Should().NotBeNull();
                recordParsed.ShouldBeEquivalentTo(new
                {
                    FileId = id,
                    Bucket = BUCKET,
                    Index = 0,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
                recordParsed.Fields.Count().Should().Be(36);
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }
    }
}
