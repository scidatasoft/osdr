using FluentAssertions;
using MassTransit;
using Ploeh.AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.MassTransit.Extensions;
using Sds.OfficeProcessor.Domain.Commands;
using Sds.OfficeProcessor.Domain.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.OfficeProcessor.Tests
{
    public class ConvertToPdfFailedEvent : ConvertToPdfFailed
    {
        public string Message { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class BlobTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture _fixture;

        public BlobTests(ParseFileTestFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_nonexistent_file_should_publish_one_ConvertationToPdfFailed_event([Frozen]ConvertToPdfFailedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = NewId.NextGuid();

                await _fixture.Harness.Bus.Publish<ConvertToPdf>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                await _fixture.Harness.Published.Any<ConvertToPdfFailed>();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<ConvertToPdfFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(new
                {
                    FileId = expectedEvent.Id,
                    Bucket = BUCKET,
                    Index = 0,
                    UserId = expectedEvent.UserId
                },
                    options => options.ExcludingMissingMembers()
                );
                failed.Message.Should().StartWith($"Cannot convert file to pdf from bucket {BUCKET} with Id {blobId}. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}