using FluentAssertions;
using MassTransit;
using Sds.MassTransit.Extensions;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalFileParser.Tests
{
    public partial class ParseChemicalFileTests : IClassFixture<ParseFileTestFixture>
    {
        [Fact]
        public async Task ParseDrugBankCorruptedDatasetTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("DrugBank_50_corrupted.sdf", Resource.DrugBank_50_corrupted, "chemical/x-mdl-sdfile", BUCKET);

                await fixture.Harness.Bus.Publish<ParseFile>(new
                {
                    Id = id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                });

                var res = await fixture.Harness.WaitWhileAllProcessed();
                res.Should().BeTrue();

                var allEvents = fixture.Harness.Published.ToList();

                allEvents.Select<FileParsed>().Count().Should().Be(1);
                allEvents.Select<RecordParsed>().Count().Should().Be(42);
                allEvents.Select<RecordParseFailed>().Count().Should().Be(5);

                var fileParsed = allEvents.Select<FileParsed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    ParsedRecords = 42,
                    FailedRecords = 5,
                    TotalRecords = 47,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
                fileParsed.Fields.Count().Should().Be(44);

                var recordParseFailed = allEvents.Select<RecordParseFailed>().FirstOrDefault();
                recordParseFailed.Should().NotBeNull();
                recordParseFailed.ShouldBeEquivalentTo(new
                {
                    FileId = id,
                    Index = 0,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
            }
            finally
            {
                await fixture.Harness.Stop();
            }

            //Assert.Equal(42, fixture.AllEvents.Count(e => e is RecordParsed));
            //Assert.Equal(5, fixture.AllEvents.Count(e => e is RecordParseFailed));
            //Assert.Equal(1, fixture.AllEvents.Count(e => e is FileParsed));

            //var fileParsedEvent = fixture.AllEvents.Single(e => e is FileParsed) as FileParsed;
            //Assert.Equal(42, fileParsedEvent.TotalRecords);
            //Assert.Equal(id, fileParsedEvent.Id);
            //Assert.Equal(correlationId, fileParsedEvent.CorrelationId);
            //Assert.Equal(fixture.UserId, fileParsedEvent.UserId);
            //Assert.NotNull(fileParsedEvent.Fields);
            //Assert.Equal(44, fileParsedEvent.Fields.Count());
        }
    }
}
