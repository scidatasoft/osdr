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
        public async Task CorrelationIdAndUserIdTest()
        {
            try
            {
                await fixture.Harness.Start();

                var blobId = await fixture.BlobStorage.AddFileAsync("Aspirin.mol", Resource.Aspirin, "chemical/x-mdl-molfile", BUCKET);
                var correlationId = NewId.NextGuid();
                var id = NewId.NextGuid();

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

                var recordParsed = allEvents.Select<RecordParsed>().FirstOrDefault();
                recordParsed.Should().NotBeNull();
                recordParsed.ShouldBeEquivalentTo(new
                {
                    CorrelationId = correlationId,
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());

                var fileParsed = allEvents.Select<FileParsed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
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
