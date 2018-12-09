using FluentAssertions;
using AutoFixture.Xunit2;
using Sds.ChemicalFileParser.Tests;
using Sds.MassTransit.Extensions;
using Sds.SpectraFileParser.Domain.Commands;
using Sds.SpectraFileParser.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sds.SpectraFileParser.Tests
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

    public class ValidJdxParseTests : IClassFixture<ParseFileTestFixture>
    {
        private const string BUCKET = "UnitTests";

        private readonly ParseFileTestFixture _fixture;

        public ValidJdxParseTests(ParseFileTestFixture fixture)
        {
            this._fixture = fixture;
        }

        [Theory, AutoData]
        public async Task Send_command_to_parse_valid_jdx_should_publish_one_RecordParsed_and_one_FileParsed_event([Frozen]FileParsedEvent expectedEvent)
        {
            try
            {
                await _fixture.Harness.Start();

                var blobId = await _fixture.BlobStorage.AddFileAsync("2-Methyl-1-Propanol.jdx", Resource._2_Methyl_1_Propanol, "chemical/x-jcamp-dx", BUCKET);

                await _fixture.Harness.Bus.Publish<ParseFile>(new
                {
                    Id = expectedEvent.Id,
                    Bucket = BUCKET,
                    BlobId = blobId,
                    CorrelationId = expectedEvent.CorrelationId,
                    UserId = expectedEvent.UserId
                });

                var res = await _fixture.Harness.WaitWhileAllProcessed();
                res.Should().BeTrue();

                var allEvents = _fixture.Harness.Published.ToList();

                var parsed = allEvents.Select<RecordParsed>().FirstOrDefault();
                parsed.Should().NotBeNull();
                parsed.Should().BeEquivalentTo(new
                {
                    FileId = expectedEvent.Id,
                    Bucket = BUCKET,
                    Index = 0L,
                    UserId = expectedEvent.UserId
                },
                    options => options.ExcludingMissingMembers()
                );
                parsed.Fields.Count().Should().Be(8);
            }
            finally
            {
                await _fixture.Harness.Stop();
            }
        }

        //[Theory, AutoData]
        public async Task Send_command_to_parse_valid_jdx_with_more_than_2_records_should_publish_one_FileParseFailed_event([Frozen]FileParseFailed expectedEvent)
        {
            var blobId = await _fixture.BlobStorage.AddFileAsync("acetophenone.jdx", Resource.acetophenone, "chemical/x-jcamp-dx", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Should().ContainSingle(e => e is FileParseFailed);

            //var @event = fixture.AllEvents.Single();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        //[Theory, AutoData]
        public async Task Send_command_to_parse_invalid_jdx_should_publish_one_FileParseFailed_event([Frozen]FileParseFailed expectedEvent)
        {
            var blobId = await _fixture.BlobStorage.AddFileAsync("2-Methyl-1-Propanol_modified.jdx", Resource._2_Methyl_1_Propanol_modified, "chemical/x-jcamp-dx", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Should().ContainSingle(e => e is FileParseFailed);

            //var @event = fixture.AllEvents.Single();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        //[Theory, AutoData]
        public async Task Send_command_to_parse_trash_jdx_should_publish_one_FileParseFailed_event([Frozen]FileParseFailed expectedEvent)
        {
            var blobId = await _fixture.BlobStorage.AddFileAsync("non_jdx.jdx", Resource.non_jdx, "chemical/x-jcamp-dx", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Should().ContainSingle(e => e is FileParseFailed);

            //var @event = fixture.AllEvents.Single();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        //[Theory, AutoData]
        public async Task Send_command_to_parse_empty_jdx_should_publish_one_FileParseFailed_event([Frozen]FileParseFailed expectedEvent)
        {
            var blobId = await _fixture.BlobStorage.AddFileAsync("empty.jdx", Resource.empty, "chemical/x-jcamp-dx", BUCKET);

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Should().ContainSingle(e => e is FileParseFailed);

            //var @event = fixture.AllEvents.Single();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );
        }

        //[Theory, AutoData]
        public async Task Send_command_to_parse_nonexistent_jdx_should_publish_one_FileParseFailed_event([Frozen]FileParseFailed expectedEvent)
        {
            var blobId = Guid.NewGuid();

            //await fixture.Bus.Send(new ParseFile(expectedEvent.Id, expectedEvent.CorrelationId, expectedEvent.UserId, BUCKET, blobId));

            //fixture.AllEvents.Should().ContainSingle(e => e is FileParseFailed);

            //var @event = fixture.AllEvents.Single();

            //@event.ShouldBeEquivalentTo(expectedEvent,
            //    options => options
            //        .Excluding(p => p.TimeStamp)
            //        .Excluding(p => p.Version)
            //    );

            await Task.CompletedTask;
        }
    }
}
