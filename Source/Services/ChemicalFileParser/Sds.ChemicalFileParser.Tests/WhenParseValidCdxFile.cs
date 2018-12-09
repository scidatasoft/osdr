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
        [Fact]
        public async Task ParseCdxTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("125_11Mos.cdx", Resource._125_11Mos, "chemical/x-cdx", BUCKET);

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

                allEvents.Where(e => e.MessageType == typeof(RecordParsed)).Count().Should().Be(3);

                var fileParsed = allEvents.Select<FileParsed>().SingleOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    TotalRecords = 3,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }

        [Fact]
        public async Task ParseCdxWithReactionsTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("_10000_10Mos.cdx", Resource._10000_10Mos, "chemical/x-cdx", BUCKET);

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

                allEvents.Where(e => e.MessageType == typeof(RecordParsed)).Count().Should().Be(2);
                allEvents.Where(e => e.MessageType == typeof(RecordParseFailed)).Count().Should().Be(1);

                var fileParsed = allEvents.Select<FileParsed>().SingleOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    TotalRecords = 3,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }
    }
}
