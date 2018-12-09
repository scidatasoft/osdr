using AutoFixture.Xunit2;
using FluentAssertions;
using MassTransit.Testing;
using Sds.ChemicalFileParser.Tests;
using Sds.CrystalFileParser.Domain.Commands;
using Sds.CrystalFileParser.Domain.Events;
using Sds.CrystalFileParser.Processing.CommandHandlers;
using Sds.MassTransit.Extensions;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.CrystalFileParser.Tests
{
    public class FileParsedEvent : FileParsed
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public long TotalRecords { get; set; }
        public IEnumerable<string> Fields { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class FileParseFailedEvent : FileParseFailed
    {
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public string Message { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class ValidCifParseTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";
        ConsumerTestHarness<ParseFileCommandHandler> _consumer;
        BusTestHarness _harness;
        IBlobStorage _blobStorage;

        public ValidCifParseTests(ParseFileTestFixture fixture)
        {
            _blobStorage = fixture.BlobStorage;
            var settings = ConfigurationManager.AppSettings.TestHarnessSettings();
            _harness = new InMemoryTestHarness();
            _harness.TestTimeout = TimeSpan.FromSeconds(settings.Timeout);
            _consumer = _harness.Consumer(() => new ParseFileCommandHandler(fixture.BlobStorage));
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_valid_cif_should_publish_one_RecordParsed_and_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("1100110.cif", Resource._1100110, "chemical/x-cif", BUCKET);

                await _harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                _consumer.Consumed.Select<ParseFile>().Any();
                _harness.Published.Select<FileParsed>().Any();

                var allEvents = _harness.Published.ToList();

                var parsed = allEvents.Select<RecordParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.ShouldBeEquivalentTo(new
                {
                    FileId = expectedEvent.Id,
                    Bucket = BUCKET,
                    Index = 0L,
                    expectedEvent.UserId
                },
                    options => options.ExcludingMissingMembers()
                );
                parsed.Fields.Count().Should().Be(22);
            }
            finally
            {
                await _harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_invalid_cif_should_publish_one_RecordParsed_and_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("1100110.cif", Resource._1100110, "chemical/x-cif", BUCKET);

                await _harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                var res = _consumer.Consumed.Select<ParseFile>().Any();
                res.Should().BeTrue();

                var allEvents = _harness.Published.ToList();

                var parsed = allEvents.Select<FileParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.TotalRecords)
                        .Excluding(p => p.Fields)
                );
                parsed.Fields.Count().Should().Be(22);
                parsed.TotalRecords.Should().Be(1);
            }
            finally
            {
                await _harness.Stop();
            }
        }

        // TODO: check why bad CIF file may be parsed 
        [Theory, AutoData]
        public async Task Send_command_to_parse_trash_cif_should_publish_one_RecordParsed_and_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("non_cif.cif", Resource.non_cif, "chemical/x-cif", BUCKET);

                await _harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                var res = _consumer.Consumed.Select<ParseFile>().Any();
                res.Should().BeTrue();

                var allEvents = _harness.Published.ToList();

                var parsed = allEvents.Select<FileParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.TotalRecords)
                        .Excluding(p => p.Fields)
                );
                parsed.Fields.Count().Should().Be(7);
                parsed.TotalRecords.Should().Be(1);
            }
            finally
            {
                await _harness.Stop();
            }
        }

        //  TODO: this test looks odd... how we can parse empty file and get any valid result???
        [Theory, AutoData]
        public async Task Send_command_to_parse_empty_cif_should_publish_one_RecordParsed_and_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("empty.cif", Resource.empty, "chemical/x-cif", BUCKET);

                await _harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                var res = _consumer.Consumed.Select<ParseFile>().Any();

                await _harness.Published.Any<FileParsed>();

                var allEvents = _harness.Published.ToList();

                var parsed = allEvents.Select<FileParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.TotalRecords)
                        .Excluding(p => p.Fields)
                );
            }
            finally
            {
                await _harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_nonexistent_cif_should_publish_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = Guid.NewGuid();

                await _harness.InputQueueSendEndpoint.Send<ParseFile>(new
                {
                    expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    expectedEvent.CorrelationId,
                    expectedEvent.UserId
                });

                var res = _consumer.Consumed.Select<ParseFile>().Any();
                res.Should().BeTrue();

                var allEvents = _harness.Published.ToList();

                var failed = allEvents.Select<FileParseFailed>().FirstOrDefault();
                failed.Should().NotBeNull();
                failed.ShouldBeEquivalentTo(expectedEvent,
                    options => options
                        .Excluding(p => p.TimeStamp)
                        .Excluding(p => p.Message)
                );
                failed.Message.Should().StartWith($"Cannot parse crystal file from bucket {BUCKET} with Id {blobId}. Error:");
            }
            finally
            {
                await _harness.Stop();
            }
        }

    }
}
