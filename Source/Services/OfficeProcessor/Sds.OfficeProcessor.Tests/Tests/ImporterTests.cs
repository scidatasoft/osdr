using FluentAssertions;
using Ploeh.AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.Domain;
using Sds.MassTransit.Extensions;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using Sds.Storage.Blob.Core;
using Spire.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.OfficeProcessor.Tests
{
    public class ConvertedToPdfEvent : ConvertedToPdf
    {
        public string Bucket { get; set; }
        public Guid BlobId { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class MetaExtractedEvent : MetaExtracted
    {
        public string Bucket { get; set; }
        public Guid BlobId { get; set; }
        public IEnumerable<Property> Meta { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class ImporterTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture _fixture;

        public ImporterTests(ParseFileTestFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_docx_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Hexahedron_kk_kc_kk_rm2_kk_docx.docx", Resource.Hexahedron_kk_kc_kk_rm2_kk_docx, "application/msword", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_odt_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Hexahedron_kk_kc_kk_rm2_kk_odt.odt", Resource.Hexahedron_kk_kc_kk_rm2_kk_odt, "application/vnd.oasis.opendocument.text", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_xls_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("key_journal_set_xls.xls", Resource.key_journal_set_xls, "application/x-msexcel", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_xlsx_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("key_journal_set_xlsx.xlsx", Resource.key_journal_set_xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }


        [Theory, AutoData]
        public async Task Send_command_to_parse_ods_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("key_journal_set_ods.ods", Resource.key_journal_set_ods, "application/vnd.oasis.opendocument.spreadsheet", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }


        [Theory, AutoData]
        public async Task Send_command_to_parse_ppt_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Soton_April_2013ppt.ppt", Resource.Soton_April_2013ppt, "application/mspowerpoint", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_pptx_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Soton_April_2013pptx.pptx", Resource.Soton_April_2013pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>();

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_odp_should_publish_one_ConvertedToPdf_event([Frozen]ConvertedToPdfEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Soton_April_2013odp.odp", Resource.Soton_April_2013odp, "application/vnd.oasis.opendocument.presentation", BUCKET);

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertedToPdf>(TimeSpan.FromSeconds(30));

                var allEvents = _fixture.Harness.Published.ToList();

                var converted = allEvents.Select<ConvertedToPdf>().SingleOrDefault();
                converted.Should().NotBeNull();
                converted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_extract_meta_from_xls_should_publish_one_MetaExtracted_event([Frozen]MetaExtractedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("key_journal_set_xls.xls", Resource.key_journal_set_xls, "application/pdf", BUCKET);

                await _fixture.Harness.Bus.Publish<ExtractMeta>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<MetaExtracted>();

                var allEvents = _fixture.Harness.Published.ToList();

                var extracted = allEvents.Select<MetaExtracted>().SingleOrDefault();
                extracted.Should().NotBeNull();
                extracted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
                extracted.Meta.Count().Should().Be(20);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_extract_meta_from_ppt_should_publish_one_MetaExtracted_event([Frozen]MetaExtractedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("Soton_April_2013ppt.ppt", Resource.Soton_April_2013ppt, "application/pdf", BUCKET);

                await _fixture.Harness.Bus.Publish<ExtractMeta>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<MetaExtracted>();

                var allEvents = _fixture.Harness.Published.ToList();

                var extracted = allEvents.Select<MetaExtracted>().SingleOrDefault();
                extracted.Should().NotBeNull();
                extracted.ShouldBeEquivalentTo(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    UserId = expectedEvent.UserId,
                    CorrelationId = expectedEvent.CorrelationId
                },
                    options => options.ExcludingMissingMembers()
                );
                extracted.Meta.Count().Should().Be(13);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}
