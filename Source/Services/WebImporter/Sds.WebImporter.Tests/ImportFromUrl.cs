using FluentAssertions;
using MassTransit;
using Sds.ChemicalFileParser.Domain;
using Sds.MassTransit.Extensions;
using Sds.WebImporter.Domain;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.WebImporter.Tests
{
    public partial class ImportFromUrl : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture fixture;

        public ImportFromUrl(ParseFileTestFixture fixture)
        {
            this.fixture = fixture;
        }


        [Fact]
        public async Task ImportFromValidWikipediaUrl()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();

                var url = "https://en.wikipedia.org/wiki/Aspirin";

                await fixture.Harness.Bus.Publish<ProcessWebPage>(new
                {
                    Id = id,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId,
                    Bucket = BUCKET,
                    Url = url
                });

                await fixture.Harness.Published.Any<WebPageProcessed>();

                var importedEvent = fixture.Harness.Published.Where(e => e is WebPageProcessed).Select(e => e as WebPageProcessed).Single();

                await fixture.Harness.Bus.Publish<ParseWebPage>(new
                {
                    Id = id,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId,
                    Bucket = BUCKET,
                    BlobId = importedEvent.BlobId
                });

                var allEvents = fixture.Harness.Published.ToList();

                var recordParsedEvents = allEvents.Select<RecordParsed>().ToList();

                recordParsedEvents.Count.Should().ShouldBeEquivalentTo(1);
                recordParsedEvents.Select(e => e.UserId).Should().OnlyContain(x => x == fixture.UserId);
                recordParsedEvents.Select(e => e.Bucket).Should().OnlyContain(x => x == BUCKET);
                recordParsedEvents.Select(e => e.UserId).Should().OnlyContain(x => x == importedEvent.BlobId);
                recordParsedEvents.Select(e => e.CorrelationId).Should().OnlyContain(x => x == correlationId);

                var webPageParsed = allEvents.Select<WebPageParsed>().SingleOrDefault();
                webPageParsed.Should().NotBeNull();
                webPageParsed.ShouldBeEquivalentTo(new
                {
                    TotalRecords = recordParsedEvents.Count,
                    FileId = id,
                    CorrelationId = correlationId,
                    Id = id,
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
        public async Task ImportFromInvalidUrl()
        {
            await fixture.Harness.Start();

            var id = NewId.NextGuid();
            var correlationId = NewId.NextGuid();
            
            var urls = new List<string> { "https://en.wikipedia.org/wilki/Aspirin",
                                            "http://parts.chemspider.com/JSON.ashx?op=GetRecordsAsCompounds&csid28s[0]=1n23&csi82ds[1]=12n4"};
            foreach (var url in urls)
            {
                await fixture.Harness.Bus.Publish<ProcessWebPage>(new
                {
                    Id = id,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId,
                    Bucket = BUCKET,
                    Url = url
                });
            }

            await fixture.Harness.Published.Any<WebPageProcessFailed>();

            var processFailed = fixture.Harness.Published.ToList().Select<WebPageProcessFailed>().ToList();
            processFailed.Select(e => e.UserId).Should().OnlyContain(x => x == fixture.UserId);
            processFailed.Select(e => e.CorrelationId).Should().OnlyContain(x => x == correlationId);
        }
    }
}
