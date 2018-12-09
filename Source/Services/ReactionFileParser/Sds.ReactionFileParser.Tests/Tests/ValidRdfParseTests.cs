using FluentAssertions;
using AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.MassTransit.Extensions;
using Sds.ReactionFileParser.Domain.Commands;
using Sds.ReactionFileParser.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MassTransit.Testing;
using Sds.ReactionFileParser.Processing.CommandHandlers;
using Sds.Storage.Blob.Core;

namespace Sds.ReactionFileParser.Tests
{
    public class FileParsedEvent : FileParsed
    {
        public long TotalRecords { get; set; }
        public IEnumerable<string> Fields { get; set; }
        public Guid Id { get; set; }
        public Guid CorrelationId { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public Guid UserId { get; set; }
    }

    public class ValidRdfParseTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        //private readonly ParseFileTestFixture _fixture;
        ConsumerTestHarness<ParseFileCommandHandler> _consumer;
        BusTestHarness _harness;
        IBlobStorage _blobStorage;

        public ValidRdfParseTests(ParseFileTestFixture fixture)
        {
            //_fixture = fixture;
            _blobStorage = fixture.BlobStorage;
            _harness = new InMemoryTestHarness();
            _consumer = _harness.Consumer(() => new ParseFileCommandHandler(fixture.BlobStorage));
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_valid_rdf_should_publish_75_RecordParsed_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("ccr0401.rdf", Resource.ccr0401, "chemical/x-mdl-rdfile", BUCKET);

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

                allEvents.Where(e => e.MessageType == typeof(RecordParsed)).Count().Should().Be(75);

                var parsed = allEvents.Select<FileParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.Should().BeEquivalentTo(expectedEvent,
                    options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Fields)
                    .Excluding(p => p.TotalRecords)
                );
                parsed.TotalRecords.Should().Be(75);
                parsed.Fields.Count().Should().Be(6);
            }
            finally
            {
                await _harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_ccr0402rdf_should_publish_105_RecordParsed_one_FileParsed_event([Frozen]FileParsedEvent expectedFileParsedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("ccr0402.rdf", Resource.ccr0402, "chemical/x-mdl-rdfile", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedFileParsedEvent.Id, expectedFileParsedEvent.CorrelationId, expectedFileParsedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Count(e => e is RecordParsed).ShouldBeEquivalentTo(105);
            //fixture.AllEvents.Count(e => e is RecordParseFailed).ShouldBeEquivalentTo(1);
            //fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            //fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            //fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);

            //var @event = fixture.AllEvents.Last();

            //@event.ShouldBeEquivalentTo(expectedFileParsedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_invalid_rdf_should_publish_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("1oir_canon_trash_modified.rdf", Resource.ccr0401_modified_trash, "chemical/x-mdl-molfile", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Count(e => e is RecordParsed).ShouldBeEquivalentTo(74);
            //fixture.AllEvents.Count(e => e is FileParseFailed).ShouldBeEquivalentTo(0);
            //fixture.AllEvents.Count(e => e is RecordParseFailed).ShouldBeEquivalentTo(1);
            //fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            //var @event = fixture.AllEvents.Last();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_trash_rdf_should_publish_one_RecordParseFailed_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("non_rdf.rdf", Resource.non_rdf, "chemical/x-mdl-rdfile", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Count(e => e is RecordParseFailed).ShouldBeEquivalentTo(1);
            //fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            //var @event = fixture.AllEvents.Last();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_empty_rdf_should_publish_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
             var blobId = await _blobStorage.AddFileAsync("empty.rdf", Resource.emptyrdf, "chemical/x-mdl-rdfile", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents
            //    .Should().ContainItemsAssignableTo<FileParseFailed>()
            //    .And.ContainSingle();

            //var @event = fixture.AllEvents.First();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_all_rdfs_should_publish_RecordParsed_event_with_properties([Frozen]FileParsedEvent expectedEvent)
        {

            var resources = new List<byte[]>() { AllRdf.ccr0401,
                AllRdf.ccr0402,
            AllRdf.ccr0403,
            AllRdf.ccr0404,
            AllRdf.mos_sample50};

            foreach (var resourse in resources)
            {
                var blobId = await _blobStorage.AddFileAsync("someFile.rdf", resourse, "chemical/x-mdl-rdfile", BUCKET);

                //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));
            }
            //var allEvents = fixture.AllEvents.Where(e => e is RecordParsed).Select(e => e as RecordParsed).ToList();

            //allEvents.All(e => e.Fields != null).ShouldBeEquivalentTo(true);

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }
    }
}
