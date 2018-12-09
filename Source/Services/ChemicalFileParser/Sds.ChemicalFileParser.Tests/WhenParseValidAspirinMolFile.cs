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
        public async Task ParseAspirinMolFileTest()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();
                var blobId = await fixture.BlobStorage.AddFileAsync("Aspirin.mol", Resource.Aspirin, "chemical/x-mdl-molfile", BUCKET);

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
                    FileId = id,
                    Bucket = BUCKET,
                    Index = 0,
                    CorrelationId = correlationId,
                    Fields = new Field[] { new Field("__NAME", "aspirina.mol") },
                    UserId = fixture.UserId
                },
                options => options.ExcludingMissingMembers());

                var fileParsed = allEvents.Select<FileParsed>().FirstOrDefault();
                fileParsed.Should().NotBeNull();
                fileParsed.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    TotalRecords = 1,
                    CorrelationId = correlationId,
                    Fields = new string[] { "__NAME" },
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
