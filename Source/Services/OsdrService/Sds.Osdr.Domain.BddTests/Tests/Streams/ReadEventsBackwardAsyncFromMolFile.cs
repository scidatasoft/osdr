using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Serialization;
using Sds.EventStore;
using Sds.Osdr.BddTests.FluentAssersions;
using Sds.Osdr.BddTests.Traits;
using Xunit;
using Xunit.Abstractions;

namespace Sds.Osdr.BddTests.Streams
{
    [Collection("OSDR Test Harness")]
    public class ReadEventsBackwardAsyncFromMolFile : OsdrTest
    {
        private Guid BlobId { get { return GetBlobId(FileId); } }
        private Guid FileId { get; set; }

        public ReadEventsBackwardAsyncFromMolFile(OsdrTestHarness fixture, ITestOutputHelper output) : base(fixture,
            output)
        {
            FileId = ProcessRecordsFile(JohnId.ToString(), "Aspirin.mol",
                new Dictionary<string, object>() {{"parentId", JohnId}}).Result;
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task Streams_GetStream_ExpectedValidOneStream()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

//            var fileView = Files.Find(new BsonDocument("_id", FileId)).FirstOrDefault() as IDictionary<string, object>;
            var events = await ((IEventStore)EventStore).ReadEventsBackwardAsync(file.Id, 0, 1);
            var oneEvent = events.First();

            oneEvent.Id.ShouldBeEquivalentTo(FileId);
            oneEvent.Version.ShouldBeEquivalentTo(1);
        }
        
        [Fact, ProcessingTrait(TraitGroup.All, TraitGroup.Chemical)]
        public async Task Streams_GetStream_ExpectedValidFullStreams()
        {
            var file = await Session.Get<RecordsFile.Domain.RecordsFile>(FileId);

            var events = await EventStore.ReadEventsBackwardAsync(file.Id);
            events.Should().HaveCount(file.Version);

            for (var i = 0; i < file.Version; i++)
            {
                var @event = events.ElementAt(i);
                @event.Version.Should().Be(file.Version - i);
            }
        }
    }
}