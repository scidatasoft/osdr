using FluentAssertions;
using Sds.ChemicalFileParser.Domain;
using Sds.ChemicalFileParser.Domain.Commands;
using Sds.MassTransit.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.ChemicalFileParser.Tests
{
    public partial class ParseChemicalFileTests : IClassFixture<ParseFileTestFixture>
    {
        [Fact]
        public async Task BlobDoesNotExistTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = Guid.NewGuid();
                var blobId = Guid.NewGuid();
                var correlationId = Guid.NewGuid();

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

                var fileParsed = allEvents.Select<FileParseFailed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    Message = $"Blob with Id {blobId} not found in bucket {BUCKET}",
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
