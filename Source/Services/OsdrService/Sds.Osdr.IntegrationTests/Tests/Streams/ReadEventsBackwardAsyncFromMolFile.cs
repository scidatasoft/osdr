using FluentAssertions;
using MongoDB.Driver;
using Sds.Osdr.IntegrationTests.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.IntegrationTests
{
    public class ReadEventsBackwardAsyncFixture
    {
        public Guid FileId { get; set; }

        public ReadEventsBackwardAsyncFixture(OsdrTestHarness harness)
        {
            FileId = harness.ProcessRecordsFile(harness.JohnId.ToString(), "Aspirin.mol", new Dictionary<string, object>() { { "parentId", harness.JohnId } }).Result;
        }
    }

    [Collection("OSDR Test Harness")]
    public class ReadEventsBackwardAsyncFromMolFile : OsdrTest, IClassFixture<ReadEventsBackwardAsyncFixture>
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ReadEventsBackwardAsyncFromMolFile(OsdrTestHarness fixture, ITestOutputHelper output, ReadEventsBackwardAsyncFixture initFixture) : base(fixture, output)
        {
            FileId = initFixture.FileId;
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task Streams_GetStream_ExpectedValidOneStream()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var events = await EventStore.ReadEventsBackwardAsync(file.Id, 0, 1);
            var oneEvent = events.First();

            oneEvent.Id.ShouldBeEquivalentTo(FileId);
            oneEvent.Version.ShouldBeEquivalentTo(1);
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task Streams_GetStream_ExpectedValidFullStreams()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var events = await Harness.EventStore.ReadEventsBackwardAsync(file.Id);
            events.Should().HaveCount(file.Version);

            for (var i = 0; i < file.Version; i++)
            {
                var @event = events.ElementAt(i);
                @event.Version.Should().Be(file.Version - i);
            }
        }
    }
}