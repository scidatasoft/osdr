using FluentAssertions;
using AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.ReactionFileParser.Domain.Commands;
using Sds.ReactionFileParser.Domain.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sds.MassTransit.Extensions;
using MassTransit.Testing;
using Sds.ReactionFileParser.Processing.CommandHandlers;

namespace Sds.ReactionFileParser.Tests
{
    public class FileParseFailedEvent : FileParseFailed
    {
        public string Message { get; set; }
        public long RecordsProcessed { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class BlobTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture _fixture;
        ConsumerTestHarness<ParseFileCommandHandler> _consumer;

        public BlobTests(ParseFileTestFixture fixture)
        {
            this._fixture = fixture;
            _consumer = _fixture.Harness.Consumer(() => new ParseFileCommandHandler(fixture.BlobStorage));
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_nonexistent_file_should_publish_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = Guid.NewGuid();

                await _fixture.Harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                var res = _consumer.Consumed.Select<ParseFile>().Any();
                res.Should().BeTrue();

                var allEvents = _fixture.Harness.Published.ToList();

                var failed = allEvents.Select<FileParseFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.Should().BeEquivalentTo(new
                {
                    expectedEvent.Id,
                    expectedEvent.UserId,
                    expectedEvent.CorrelationId
                }
                //options => options.Excluding(p => p.TimeStamp)
                //.Excluding(p => p.RecordsProcessed)
                //.Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Cannot parse reaction file from bucket {BUCKET} with Id {expectedEvent.Id}. Error:");
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }
    }
}