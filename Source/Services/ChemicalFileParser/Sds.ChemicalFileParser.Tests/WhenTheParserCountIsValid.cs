using FluentAssertions;
using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.Domain;
using Sds.MassTransit.Extensions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalFileParser.Tests
{
	public partial class ParseChemicalFileTests : IClassFixture<ParseFileTestFixture>
	{
        [Fact]
        public async Task WhenParseInvalidRingCountMolFile()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("ringcount_0.mol", Resource.ringcount_0, "chemical/x-mdl-molfile", BUCKET);

                await fixture.Harness.Bus.Publish<ParseFile>(new
                {
                    Id = id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                });

                await fixture.Harness.Published.Any<FileParsed>();

                var allEvents = fixture.Harness.Published.ToList();

                var fileParsed = allEvents.Select<FileParsed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
					FailedRecords =	1,
					ParsedRecords = 0,
					TotalRecords = 0,
                    CorrelationId = correlationId,
                    Fields = new string[] { },
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());

                allEvents.Where(e => e.MessageType == typeof(RecordParseFailed)).Count().Should().Be(1);
            }
            finally
            {
                await fixture.Harness.Stop();
            }
        }
	}
}
