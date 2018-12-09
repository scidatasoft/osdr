using FluentAssertions;
using MassTransit;
using Sds.MassTransit.Extensions;
using Sds.WebImporter.Domain.Commands;
using Sds.WebImporter.Domain.Events;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.WebImporter.PdfTests
{
    public class ImportFromUrl : IClassFixture<ConverterFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ConverterFixture fixture;

        public ImportFromUrl(ConverterFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task ImportFromGenericUrl()
        {
            try
            {
                await fixture.Harness.Start();

                var id = NewId.NextGuid();
                var correlationId = NewId.NextGuid();

                var url = "http://lifescience.opensource.epam.com/indigo/api/#loading-molecules-and-query-molecules";

                await fixture.Harness.Bus.Publish<GeneratePdfFromHtml>(new
                {

                    Id = id,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId,
                    Bucket = BUCKET,
                    Url = url
                });

                await fixture.Harness.Published.Any<PdfGenerated>();

                var pdfGenerated = fixture.Harness.Published.Where(e => e is PdfGenerated).Select(e => e as PdfGenerated).Single();

                pdfGenerated.UserId.ShouldBeEquivalentTo(fixture.UserId);
                pdfGenerated.Bucket.ShouldBeEquivalentTo(BUCKET);
                pdfGenerated.CorrelationId.ShouldBeEquivalentTo(correlationId);
                pdfGenerated.Id.ShouldBeEquivalentTo(id);

                pdfGenerated.ShouldBeEquivalentTo(new
                {
                    Id = id,
                    CorrelationId = correlationId,
                    UserId = fixture.UserId,
                    Bucket = BUCKET,
                    Url = url
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
