using FluentAssertions;
using AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.MassTransit.Extensions;
using Sds.ReactionFileParser.Domain.Commands;
using Sds.ReactionFileParser.Domain.Events;
using Sds.Storage.Blob.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MassTransit.Testing;
using Sds.ReactionFileParser.Processing.CommandHandlers;

namespace Sds.ReactionFileParser.Tests
{
    public class ValidRxnParseTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        //private readonly ParseFileTestFixture _fixture;
        ConsumerTestHarness<ParseFileCommandHandler> _consumer;
        BusTestHarness _harness;
        IBlobStorage _blobStorage;

        public ValidRxnParseTests(ParseFileTestFixture fixture)
        {
            //_fixture = fixture;
            _blobStorage = fixture.BlobStorage;
            _harness = new InMemoryTestHarness();
            _consumer = _harness.Consumer(() => new ParseFileCommandHandler(fixture.BlobStorage));
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_valid_rxn_should_publish_one_RecordParsed_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _harness.Start();

                var blobId = await _blobStorage.AddFileAsync("10001.rxn", Resource._10001, "chemical/x-mdl-rxnfile", BUCKET);

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

                allEvents.Where(e => e.MessageType == typeof(RecordParsed)).Count().Should().Be(1);

                var parsed = allEvents.Select<FileParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.Should().BeEquivalentTo(expectedEvent,
                    options => options
                    .Excluding(p => p.TimeStamp)
                    .Excluding(p => p.Fields)
                    .Excluding(p => p.TotalRecords)
                );
                parsed.TotalRecords.Should().Be(1);
            }
            finally
            {
                await _harness.Stop();
            }
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_invalid_rxn_should_publish_one_RecordParseFailed_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("10001_modified_trash.rxn", Resource._10001_modified_trash, "chemical/x-mdl-rxnfile", BUCKET);

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
        public async Task Send_command_to_parse_trash_rxn_should_publish_one_RecordParseFailed_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("non_rxn.rxn", Resource.non_rxn, "chemical/x-mdl-rxnfile", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Count(e => e is RecordParseFailed).ShouldBeEquivalentTo(1);
            //fixture.AllEvents.Count(e => e is FileParsed).ShouldBeEquivalentTo(1);

            //var @event = fixture.AllEvents.First();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_empty_rxn_should_publish_one_FileParseFailed_event([Frozen]FileParseFailedEvent expectedEvent)
        {
            var blobId = await _blobStorage.AddFileAsync("empty.rxn", Resource.emptyrxn, "chemical/x-mdl-rxnfile", BUCKET);

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
        public async Task Send_command_to_parse_all_rxns_should_publish_RecordParsed_event_with_properties([Frozen]FileParsedEvent expectedEvent)
        {

            var resources = new List<byte[]>() { AllRxn._10001,
            AllRxn._10002,
            AllRxn._10005,
            AllRxn._10006,
            AllRxn._10009,
            AllRxn._10010,
            AllRxn._10013,
            AllRxn._10014,
            AllRxn._10017,
            AllRxn._10018,
            AllRxn._10021,
            AllRxn._10022,
            AllRxn._10025,
            AllRxn._10026,
            AllRxn._10029,
            AllRxn._10030,
            AllRxn._10037,
            AllRxn._10038,
            AllRxn._10041,
            AllRxn._10042,
            AllRxn._10045,
            AllRxn._10046,
            AllRxn._10049,
            AllRxn._10050,
            AllRxn._10057,
            AllRxn._10058,
            AllRxn._10061,
            AllRxn._10062,
            AllRxn._10065,
            AllRxn._10066,
            AllRxn._10073,
            AllRxn._10074,
            AllRxn._10077,
            AllRxn._10078,
            AllRxn._10081,
            AllRxn._10082,
            AllRxn._10085,
            AllRxn._10086,
            AllRxn._10089,
            AllRxn._10090};

            foreach (var resourse in resources)
            {
                var blobId = await _blobStorage.AddFileAsync("someFile.rxn", resourse, "chemical/x-mdl-rdfile", BUCKET);

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
